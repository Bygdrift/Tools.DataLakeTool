using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Bygdrift.Tools.LogTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bygdrift.Tools.DataLakeTool
{
    /// <summary>
    /// A datalake working with messages.
    /// </summary>
    public class DataLakeQueue
    {
        private static QueueClient _queueClient;
        private string _queueName;

        /// <summary>
        /// Constructror for dataLake
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="containerName">Rules for the containername (is checked): https://www.thecodebuzz.com/azure-requestfailedexception-specified-resource-name-contains-invalid-characters/</param>
        /// <param name="log"></param>
        public DataLakeQueue(string connectionString, string containerName, Log log)
        {
            Log = log;
            ConnectionString = connectionString;
            Container = containerName.ToLower();  //Rules for the containername (Is checked when loading app.ModuleName): https://www.thecodebuzz.com/azure-requestfailedexception-specified-resource-name-contains-invalid-characters/
            if (!Container.All(o => char.IsLetterOrDigit(o)))
                throw new Exception("The containerName must only contain letters and numbers.");

            if (Container.Length < 3 || Container.Length > 24)
                throw new Exception("The containerName must be between 3 and 24 letters or numbers.");
        }

        /// <summary>
        /// Data lake connection
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// The container in used for this dataLake - primarily the moduleName but lowercase.
        /// </summary>
        public string Container { get; }

        /// <summary>
        /// Info about excecution
        /// </summary>
        public Log Log { get; }

        /// <summary>
        /// The data lake name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// By default the lowercased module name. If set, then the name is "moduleName-queueName" - all lower case.
        /// To go back to default name, just set the name to null.
        /// </summary>
        public string QueueName
        {
            get
            {
                return _queueName ??= Container;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    _queueName = Container;
                else
                {
                    if (!value.All(o => char.IsLetter(o)))
                        throw new Exception("The queueName, must only contain letters.");

                    _queueName = Container + "-" + value.ToLower();
                }
                if(_queueClient != null)
                {
                    _queueClient = new QueueClient(ConnectionString, QueueName);
                    _queueClient.CreateIfNotExists();
                }
            }
        }

        /// <summary>
        /// The client. If QueueName is not set, then the default name is ModuleName
        /// </summary>
        public QueueClient QueueClient
        {
            get
            {
                if (_queueClient == null)
                {
                    _queueClient = new QueueClient(ConnectionString, QueueName);
                    _queueClient.CreateIfNotExists();
                }
                return _queueClient;
            }
        }

        /// <summary>
        /// Adds a message
        /// </summary>
        public async Task<SendReceipt> AddMessageAsync(string message)
        {
            return (await QueueClient.SendMessageAsync(message)).Value;
        }

        /// <summary>
        /// Add multiple messages
        /// </summary>
        public async Task<IEnumerable<SendReceipt>> AddMessagesAsync(string[] messages)
        {
            var tasks = new List<Task<Azure.Response<SendReceipt>>>();
            foreach (var item in messages)
                tasks.Add(QueueClient.SendMessageAsync(item));

            await Task.WhenAll(tasks);
            return tasks.Select(o => o.Result.Value);
        }

        /// <summary>
        /// Delete all messages
        /// </summary>
        public async Task DeleteMessagesAsync()
        {
            var messages = await GetMessagesAsync();
            await DeleteMessagesAsync(messages);
        }

        /// <summary>
        /// Delete a message
        /// </summary>
        public async Task DeleteMessageAsync(QueueMessage message)
        {
            await QueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
        }

        /// <summary>
        /// Delete multiple messages
        /// </summary>
        public async Task DeleteMessagesAsync(IEnumerable<QueueMessage> messages)
        {
            if (messages == null)
                return;

            var tasks = new List<Task>();
            foreach (var message in messages)
                tasks.Add(QueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Delete the queue
        /// </summary>
        public async Task DeleteQueueAsync()
        {
           await QueueClient.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Get a message
        /// </summary>
        public async Task<QueueMessage> GetMessageAsync()
        {
            return await QueueClient.ReceiveMessageAsync();
        }

        /// <summary>
        /// Get multiple messages
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="visibilityTimeout">How long time the messages will be invisible. The time starts for all messages at the same time. Default is 30 seconds. Time should be long enough for the loop to fetch all messages before the become visible again. So if you should get i million messages ands sets of one second, the process will get the same message multiple times</param>
        /// <returns></returns>
        public async Task<IEnumerable<QueueMessage>> GetMessagesAsync(int? amount = null, TimeSpan? visibilityTimeout = null)
        {
            var tasks = new List<Task<Azure.Response<QueueMessage[]>>>();
            if (amount <= 0)
                amount = null;

            if (amount <= 32)
                return (await QueueClient.ReceiveMessagesAsync(amount, visibilityTimeout)).Value;

            var messageCounts = (await QueueClient.GetPropertiesAsync()).Value.ApproximateMessagesCount;
            if (messageCounts == 0)
                return default;

            if (amount == null || messageCounts < amount)
                amount = messageCounts;

            var take = 32;  //32 is maximum for message to retrieve per call
            var calls = Math.Ceiling((double)amount / take);

            for (int i = 1; i <= calls; i++)
            {
                if (i == calls && i * take != amount)
                    take = (int)amount % take;

                tasks.Add(QueueClient.ReceiveMessagesAsync(take, visibilityTimeout));
            }

            await Task.WhenAll(tasks);
            return tasks.SelectMany(o => o.Result.Value);
        }

        ///// <summary>
        ///// Peek a message so it doesn't turn invisible for 30 seconds
        ///// </summary>
        //public async Task<PeekedMessage> PeekMessageAsync()
        //{
        //    return await QueueClient.PeekMessageAsync();
        //}

        /// <summary>
        /// Peek multiple messages so it doesn't turn invisible for 30 seconds. Twice as fast as Getmessages but can't be deleted. You can only get up to 32 queues.
        /// </summary>
        public async Task<IEnumerable<PeekedMessage>> PeekMessagesAsync(int? amount = null)
        {
            var tasks = new List<Task<Azure.Response<PeekedMessage[]>>>();
            if (amount <= 0)
                amount = null;

            if (amount <= 32)
                return (await QueueClient.PeekMessagesAsync(amount)).Value;
            else
                return (await QueueClient.PeekMessagesAsync(32)).Value;

            ///The following is not possible. Quesus are first in, first out.
            //var messageCounts = (await QueueClient.GetPropertiesAsync()).Value.ApproximateMessagesCount;
            //if (messageCounts == 0)
            //    return default;

            //if (amount == null || messageCounts < amount)
            //    amount = messageCounts;

            //var take = 32;  //32 is maximum for message to retrieve per call
            //var calls = Math.Ceiling((double)amount / take);

            //for (int i = 1; i <= calls; i++)
            //{
            //    if (i == calls && i * take != amount)
            //        take = (int)amount % take;

            //    QueueClient.ReceiveMessagesAsync(maxMessages: take, calls: 1)


            //    tasks.Add(QueueClient.PeekMessagesAsync(maxMessages:take, calls: 1));
            //}

            //await Task.WhenAll(tasks);
            //return tasks.SelectMany(o => o.Result.Value);
        }

        /// <summary>
        /// Counts the amount of messages
        /// </summary>
        public async Task<int> MessagesCountAsync()
        {
            return (await QueueClient.GetPropertiesAsync()).Value.ApproximateMessagesCount;
        }
    }
}

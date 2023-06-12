using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataLakeToolTests
{
    /// <summary>
    /// TODO: Test at sende flere filer med samme filePath på en gang og sørg for at håndtere når den brækker ned!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// </summary>

    [TestClass]
    public class DataLakeQueueTests
    {
        private readonly DataLakeQueue queue;
        private IConfigurationRoot _config;

        public DataLakeQueueTests()
        {
            queue = new DataLakeQueue(Config["Secret--DataLakeConnectionString"], Config["ModuleName"], new Bygdrift.Tools.LogTool.Log());
        }

        /// <summary>
        /// Runs through more than 32 times and checks if all are represented end thare are no redundant data
        /// </summary>
        [TestMethod]
        public async Task Get40Messages()
        {
            await queue.DeleteMessagesAsync();

            var tasks = new List<string>();
            for (int i = 0; i < 40; i++)
                tasks.Add("Besked " + i);

            await queue.AddMessagesAsync(tasks.ToArray());

            var counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 40);

            var queues = await queue.GetMessagesAsync(null, new TimeSpan(0, 0, 5));
            var body = queues.Select(o=> o.Body.ToString()).ToList();

            var isSelected = new bool[40];
            foreach (var item in body)
            {
                var a = item.Replace("Besked ", string.Empty);
                var b = Convert.ToInt32(a);
                isSelected[b] = true;
            }

            Assert.IsFalse(isSelected.Any(o => !o));

            await queue.DeleteMessagesAsync(queues);

            counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 0);
        }


        [TestMethod]
        public async Task ManyQueues()  //There will come errors on concurrent readings. Make sure to get the errors fetched into the log.
        {
            await queue.DeleteMessagesAsync();

            var tasks = new List<string>();
            for (int i = 0; i < 1000; i++)
                tasks.Add("Besked " + i);
            
            await queue.AddMessagesAsync(tasks.ToArray());

            var counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 1000);

            var queues = await queue.GetMessagesAsync(null, new TimeSpan(0,0,5));
            await queue.DeleteMessagesAsync(queues);

            await Task.Delay(6000);

            counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 0);
        }

        [TestMethod]
        public async Task QueueChangeName()  //There will come errors on concurrent readings. Make sure to get the errors fetched into the log.
        {
            await queue.DeleteMessagesAsync();

            await queue.AddMessageAsync("Message");

            queue.QueueName = "testName";

            await queue.AddMessageAsync("Message");

            var counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 1);

            await queue.DeleteMessagesAsync();

            queue.QueueName = null;

            await queue.DeleteQueueAsync();
        }

        public IConfigurationRoot Config
        {
            get
            {
                return _config ??= new ConfigurationBuilder().SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."))
                     .AddJsonFile("local.settings.json", true, true)
                     .AddJsonFile("appsettings.json", true, true)
                     .AddJsonFile("appsettings.development.json", true, true)
                     .AddEnvironmentVariables()
                     .Build();
            }
        }
    }
}

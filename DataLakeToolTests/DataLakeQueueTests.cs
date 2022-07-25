using Bygdrift.Tools.DataLakeTool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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

        public DataLakeQueueTests()
        {
            queue = new DataLakeQueue("", "test", new Bygdrift.Tools.LogTool.Log());
        }

        [TestMethod]
        public async Task Queue()  //There will come errors on concurrent readings. Make sure to get the errors fetched into the log.
        {
            await queue.DeleteMessagesAsync();

            var tasks = new List<string>();
            for (int i = 0; i < 1000; i++)
                tasks.Add("Besked " + i);

            await queue.AddMessagesAsync(tasks.ToArray());

            var counts = await queue.MessagesCountAsync();
            Assert.IsTrue(counts == 1000);

            var queues = await queue.GetMessagesAsync();
            await queue.DeleteMessagesAsync(queues);

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
    }
}

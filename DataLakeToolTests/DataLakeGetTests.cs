using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataLakeToolTests
{
    [TestClass]
    public class DataLakeGetTests
    {
        private readonly DataLake dataLake;

        public DataLakeGetTests()
        {
            dataLake = new DataLake("", "test", new Bygdrift.Tools.LogTool.Log(), DateTime.Now);
        }

        [TestMethod]
        public void GetFile()
        {
            CleanAndAddFilesAsync().Wait();
            Assert.IsTrue(dataLake.GetFile("Refined/Test.csv").Stream.Length > 1);
            Assert.IsTrue(dataLake.GetFile("Refined/Subfolder/Subfolder/Test.csv").Stream.Length > 1);
            Assert.IsTrue(dataLake.GetFile("RefinedError/Test.csv").Stream == null);
            Assert.IsTrue(dataLake.GetFile("Refined/TestError.csv").Stream == null);
            Assert.IsTrue(dataLake.GetFile("Refined/test.csv").Stream == null);

            var firstFile = dataLake.GetFirstOrDefaultFile("Refined", FolderStructure.Path, false);
            Assert.AreEqual("Test.csv", firstFile.Value.FileName);
        }

        private async Task CleanAndAddFilesAsync()
        {
            await dataLake.DeleteDirectoryAsync("Refined");

            var csv = new Csv("Id");
            csv.AddRecord(0, 0, 1);
            await dataLake.SaveCsvAsync(csv, "Refined", "Test.csv", FolderStructure.Path);
            await dataLake.SaveCsvAsync(csv, "Refined/Subfolder/Subfolder", "Test.csv", FolderStructure.Path);
            await dataLake.SaveCsvAsync(csv, "Refined", "Test2.csv", FolderStructure.Path);
        }
    }
}

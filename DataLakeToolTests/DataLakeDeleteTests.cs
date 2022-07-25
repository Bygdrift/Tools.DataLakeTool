using Bygdrift.Tools.DataLakeTool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLakeToolTests
{
    [TestClass]
    public class DataLakeDeleteTests
    {
        private readonly DataLake dataLake;

        public DataLakeDeleteTests()
        {
            dataLake = new DataLake("", "test", new Bygdrift.Tools.LogTool.Log(), DateTime.Now);
        }

        [TestMethod]
        public async Task CreateAndDeleteBasePath()
        {
            await dataLake.CreateDirectoryAsync("Refined", FolderStructure.Path);

            Assert.IsTrue(dataLake.BasePathExists("Refined"));

            await dataLake.DeleteDirectoryAsync("Refined", FolderStructure.Path);
            Assert.IsFalse(dataLake.BasePathExists("Refined"));
        }

        [TestMethod]
        public async Task DeleteOlderThanDays()
        {
            await dataLake.SaveStringAsync("hej", "DeleteOlderThanDays", "data.txt", FolderStructure.DateTimePath);
            dataLake.LocalTime = DateTime.Now.AddMonths(-6);
            await dataLake.SaveStringAsync("hej", "DeleteOlderThanDays", "data.txt", FolderStructure.DateTimePath);

            var a = dataLake.GetDirectories(null);
            await dataLake.DeleteDirectoriesOlderThanDaysAsync(null, 100);

            await dataLake.DeleteDirectoryAsync("DeleteOlderThanDays", FolderStructure.Path);
        }

        [TestMethod]
        public async Task CreateAndDeleteRoot()
        {
            await dataLake.CreateDirectoryAsync("Refined", FolderStructure.Path);
            Assert.IsTrue(dataLake.BasePathExists("Refined"));

            await dataLake.DeleteDirectoryAsync(null, FolderStructure.Path);
            Assert.IsFalse(dataLake.BasePathExists("Refined"));
        }
    }
}

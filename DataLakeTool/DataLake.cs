﻿using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Bygdrift.Tools.LogTool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bygdrift.Tools.DataLakeTool
{
    public partial class DataLake
    {
        private DataLakeServiceClient _dataLakeServiceClient;
        private string storageAccountName;
        private string storageAccountKey;

        /// <summary>
        /// Constructror for dataLake
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="containerName">Rules for the containername (is checked): https://www.thecodebuzz.com/azure-requestfailedexception-specified-resource-name-contains-invalid-characters/</param>
        /// <param name="log"></param>
        /// <param name="localTime"></param>
        public DataLake(string connectionString, string containerName, Log log, DateTime localTime)
        {
            Log = log;
            LocalTime = localTime;
            SetConnectionString(connectionString);
            Container = containerName.ToLower();  //Rules for the containername (Is checked when loading app.ModuleName): https://www.thecodebuzz.com/azure-requestfailedexception-specified-resource-name-contains-invalid-characters/
            if (!Container.All(o => char.IsLetterOrDigit(o)))
                throw new Exception("The containerName must only contain letters and numbers.");

            if (Container.Length < 3 || Container.Length > 24)
                throw new Exception("The containerName must be between 3 and 24 letters or numbers.");
        }

        /// <summary>
        /// Info about excecution
        /// </summary>
        public Log Log { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LocalTime { get; set; }

        /// <summary>
        /// The serviceClient for contacting the DataLake
        /// </summary>
        public DataLakeServiceClient DataLakeServiceClient
        {
            get
            {
                if (_dataLakeServiceClient == null)
                {
                    var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
                    _dataLakeServiceClient = new DataLakeServiceClient(ServiceUri, sharedKeyCredential);
                }
                return _dataLakeServiceClient;
            }
        }

        /// <summary>
        /// The container in used for this dataLake - primarily the moduleName but lowercase.
        /// </summary>
        public string Container { get; }

        /// <summary>
        /// The dataLake uri
        /// </summary>
        public Uri ServiceUri { get; private set; }

        private void SetConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "The dataLake connectionString, has to be set.");

            var blobEndpoint = "";
            var endpointProtocol = "";
            var endpointSuffix = "";
            foreach (var item in connectionString.Split(';'))
            {
                if (!string.IsNullOrEmpty(item))
                    try
                    {
                        var count = item.IndexOf('=');
                        var pair = new string[] { item[..count], item[(count + 1)..] };
                        if (pair.Length == 2)
                        {
                            switch (pair[0].ToLower())
                            {
                                case "defaultendpointsprotocol": endpointProtocol = pair[1]; break;
                                case "accountname": storageAccountName = pair[1]; break;
                                case "accountkey": storageAccountKey = pair[1]; break;
                                case "endpointsuffix": endpointSuffix = pair[1]; break;
                                case "blobendpoint": blobEndpoint = pair[1]; break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("There´s an error in the Datalake connectionstring", e);
                    }
            }

            if (string.IsNullOrEmpty(storageAccountName) || string.IsNullOrEmpty(storageAccountKey))
                throw new Exception("The value for DataLakeConnectionString, is not correct and is missing one or more parameters.");
            else if (!string.IsNullOrEmpty(blobEndpoint)) //Localhost: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator
                ServiceUri = new Uri(blobEndpoint);
            else if (string.IsNullOrEmpty(endpointProtocol) || string.IsNullOrEmpty(endpointSuffix))
                throw new Exception("The value for DataLakeConnectionString, is not correct and is missing one or more parameters.");
            else
                ServiceUri = new Uri($"{endpointProtocol}://{storageAccountName}.dfs.{endpointSuffix}/");
        }

        internal string CreateDatePath(string basePath, bool savePerHour)
        {
            var pathParts = new List<string> { basePath, LocalTime.ToString("yyyy"), LocalTime.ToString("MM"), LocalTime.ToString("dd") };
            if (savePerHour)
                pathParts.Add(LocalTime.ToString("HH"));

            var res = string.Join('/', pathParts);
            return res;
        }

        /// <summary>
        /// Creates a path
        /// </summary>
        /// <param name="basePath">The bais path</param>
        /// <param name="folderStructure">What kind of path to create</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetBasePath(string basePath, FolderStructure folderStructure)
        {
            if (folderStructure == FolderStructure.Path)
                return basePath;
            if (folderStructure == FolderStructure.DatePath)
                return CreateDatePath(basePath, false);
            if (folderStructure == FolderStructure.DateTimePath)
                return CreateDatePath(basePath, true);

            throw new Exception("Not implemented");
        }
    }

    /// <summary>
    /// The kind of folderstructure
    /// </summary>
    public enum FolderStructure
    {
        /// <summary>Saved by a path</summary>
        Path,
        /// <summary>Each file saved in a folder with the given date</summary>
        DatePath,
        /// <summary>Each file saved in a folder with the given date and hour</summary>
        DateTimePath
    }
}

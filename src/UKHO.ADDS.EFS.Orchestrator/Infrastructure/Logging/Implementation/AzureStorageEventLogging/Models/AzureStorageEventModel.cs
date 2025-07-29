using System;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Models
{
    public class AzureStorageEventModel
    {
        /// <summary>
        ///     The default constructor
        /// </summary>
        /// <param name="fileFullName">The file full name</param>
        /// <param name="data"> The data ( string)></param>
        public AzureStorageEventModel(string fileFullName, string data)
        {
            FileFullName = string.IsNullOrEmpty(fileFullName) ? throw new NullReferenceException(nameof(FileFullName)) : fileFullName;
            Data = string.IsNullOrEmpty(data) ? throw new NullReferenceException(nameof(Data)) : data;
        }

        public string Data { get; private set; }
        public string FileFullName { get; private set; }
    }
}
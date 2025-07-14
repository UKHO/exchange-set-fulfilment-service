namespace UKHO.ADDS.EFS.Configuration.Namespaces
{
    public static class StorageConfiguration
    {
        // TODO Split between "account" and "storage", these refer to two different things

        public const string StorageName = "efs-storage";

        public const string QueuesName = "efs-queues";

        public const string TablesName = "efs-tables";

        public const string BlobsName = "efs-blobs";

        // NB: Lowercase concatenated names are used to ensure compatibility and consistency across Azure storage

        public const string JobRequestQueueName = "jobrequest";

        public const string S100BuildRequestQueueName = "s100buildrequest";

        public const string S100BuildResponseQueueName = "s100buildresponse";

        public const string S100BuildContainer = "s100build";

        public const string S63BuildRequestQueueName = "s63buildrequest";

        public const string S63BuildResponseQueueName = "s63buildresponse";

        public const string S63BuildContainer = "s63build";

        public const string S57BuildRequestQueueName = "s57buildrequest";

        public const string S57BuildResponseQueueName = "s57buildresponse";

        public const string S57BuildContainer = "s57build";

        public const string DataStandardTimestampTable = "datastandardtimestamp";

        public const string JobTable = "job";

        public const string BuildMementoTable = "buildmemento";

        public const string JobHistoryTable = "jobhistory";
    }
}

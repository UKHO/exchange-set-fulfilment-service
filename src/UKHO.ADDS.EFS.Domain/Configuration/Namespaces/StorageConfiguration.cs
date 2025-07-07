namespace UKHO.ADDS.EFS.Configuration.Namespaces
{
    public static class StorageConfiguration
    {
        public const string StorageName = "efs-storage";

        public const string QueuesName = "efs-queues";

        public const string TablesName = "efs-tables";

        public const string BlobsName = "efs-blobs";

        // NB: Lowercase concatenated names are used to ensure compatibility and consistency across Azure storage

        public const string JobRequestQueueName = "jobrequest";

        public const string S100BuildRequestQueueName = "s100buildrequest";

        public const string S100BuildResponseQueueName = "s100buildresponse";

        public const string S100JobContainer = "s100job";

        public const string S63BuildRequestQueueName = "s63buildrequest";

        public const string S63BuildResponseQueueName = "s63buildresponse";

        public const string S63JobContainer = "s63job";

        public const string S57BuildRequestQueueName = "s57buildrequest";

        public const string S57BuildResponseQueueName = "s57buildresponse";

        public const string S57JobContainer = "s57job";

        public const string ExchangeSetTimestampTable = "exchangesettimestamp";

        public const string ExchangeSetBuildStatusTable = "exchangesetbuildstatus";

        public const string ExchangeSetJobTypeTable = "exchangesetjobtype";
    }
}

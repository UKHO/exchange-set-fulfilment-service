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

        public const string ExchangeSetTimestampTable = "exchangesettimestamp";

        public const string ExchangeSetBuildStatusTable = "exchangesetbuildstatus";
    }
}

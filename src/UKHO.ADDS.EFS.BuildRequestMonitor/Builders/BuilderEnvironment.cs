namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class BuilderEnvironment
    {
        public string RequestQueueName { get; set; }

        public string ResponseQueueName { get; set; }

        public string QueueEndpoint { get; set; }

        public string BlobContainerName { get; set; }

        public string BlobEndpoint { get; set; }

        public string AddsEnvironment { get; set; }

        public int MaxRetryAttempts { get; set; }

        public int RetryDelayMilliseconds { get; set; }

        public string FileShareEndpoint { get; set; }

        public string FileShareHealthEndpoint { get; set; }

        public int ConcurrentDownloadLimitCount { get; set; }
    }
}

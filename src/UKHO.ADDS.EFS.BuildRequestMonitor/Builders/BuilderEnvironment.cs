namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class BuilderEnvironment
    {
        public required string RequestQueueName { get; set; }

        public required string ResponseQueueName { get; set; }

        public required string QueueEndpoint { get; set; }

        public required string BlobContainerName { get; set; }

        public required string BlobEndpoint { get; set; }

        public required string AddsEnvironment { get; set; }

        public required int MaxRetryAttempts { get; set; }

        public required int RetryDelayMilliseconds { get; set; }

        public required string FileShareEndpoint { get; set; }

        public required string FileShareHealthEndpoint { get; set; }

        public required int ConcurrentDownloadLimitCount { get; set; }
    }
}

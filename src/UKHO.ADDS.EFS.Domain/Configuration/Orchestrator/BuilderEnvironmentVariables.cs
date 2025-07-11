﻿namespace UKHO.ADDS.EFS.Configuration.Orchestrator
{
    public class BuilderEnvironmentVariables
    {
        public const string FileShareEndpoint = "FSS_ENDPOINT";

        public const string FileShareHealthEndpoint = "FSS_ENDPOINT_HEALTH";

        public const string RequestQueueName = "REQUEST_QUEUE_NAME";

        public const string ResponseQueueName = "RESPONSE_QUEUE_NAME";

        public const string QueueConnectionString = "QUEUE_CONNECTION_STRING";

        public const string BlobContainerName = "BLOB_CONTAINER_NAME";

        public const string BlobConnectionString = "BLOB_CONNECTION_STRING";

        public const string AddsEnvironment = "ADDS_ENVIRONMENT";

        public const string MaxRetryAttempts = "MAX_RETRY_ATTEMPTS";

        public const string RetryDelayMilliseconds = "RETRY_DELAY_MS";

        public const string ConcurrentDownloadLimitCount = "CONCURRENT_DOWNLOAD_LIMIT_COUNT";
    }
}

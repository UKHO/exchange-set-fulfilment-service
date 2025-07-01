namespace UKHO.ADDS.EFS.Configuration.Orchestrator
{
    public class BuilderEnvironmentVariables
    {
        // TODO Remove these once builder refactored
        public const string JobId = "JOB_ID";
        public const string FileShareEndpoint = "FSS_ENDPOINT";
        public const string BuildServiceEndpoint = "BUILD_SVC_ENDPOINT";
        public const string OtlpEndpoint = "OTLP_ENDPOINT";
        public const string BatchId = "BATCH_ID";
        public const string WorkspaceKey = "WORKSPACE_KEY";


        public const string RequestQueueName = "REQUEST_QUEUE_NAME";

        public const string ResponseQueueName = "RESPONSE_QUEUE_NAME";

        public const string QueueConnectionString = "QUEUE_CONNECTION_STRING";

        public const string BlobContainerName = "BLOB_CONTAINER_NAME";

        public const string BlobConnectionString = "BLOB_CONNECTION_STRING";

        public const string AddsEnvironment = "ADDS_ENVIRONMENT";

        public const string MaxRetryAttempts = "MAX_RETRY_ATTEMPTS";

        public const string RetryDelayMilliseconds = "RETRY_DELAY_MS";
    }
}

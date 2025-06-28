namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class BuilderEnvironment
    {
        public string RequestQueueName { get; set; }

        public string ResponseQueueName { get; set; }

        public string QueueConnectionString { get; set; }

        public string BlobContainerName { get; set; }

        public string BlobConnectionString { get; set; }

        public string AddsEnvironment { get; set; }
    }
}

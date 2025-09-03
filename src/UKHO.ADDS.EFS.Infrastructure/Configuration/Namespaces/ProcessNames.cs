namespace UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces
{
    public static class ProcessNames
    {
        public const string ConfigurationService = "efs-appconfig";

        public const string OrchestratorService = "efs-orchestrator";

        public const string S100Builder = "efs-builder-s100";

        public const string S63Builder = "efs-builder-s63";

        public const string S57Builder = "efs-builder-s57";

        public const string MockService = "adds-mocks-efs";

        public const string RedisCache = "efs-redis";

        public const string RequestMonitorService = "efs-local-request-monitor";

        public const string FileShareService = "FileShare"; // NB: Must agree with the key in external-services.json

        public const string SalesCatalogueService = "SalesCatalogue"; // NB: Must agree with the key in external-services.json
    }
}

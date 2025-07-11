﻿namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging
{
    internal class ConfigurationLogView
    {

        public required string JobId { get; init; }
        public required string BatchId { get; init; }
        public required string FileShareEndpoint { get; init; }
        public required string FileShareHealthEndpoint { get; init; }
        public required string WorkspaceAuthenticationKey { get; init; }
        public required string ExchangeSetNameTemplate { get; init; }
    }
}

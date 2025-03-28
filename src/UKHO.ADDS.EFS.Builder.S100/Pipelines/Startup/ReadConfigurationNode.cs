﻿using Serilog;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class ReadConfigurationNode : ExchangeSetPipelineNode
    {
        private const string DebugRequestId = "DebugRequestId";

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            Log.Information("UKHO ADDS EFS S100 Builder");
            Log.Information($"Machine ID      : {Environment.MachineName}");

            var requestId = GetEnvironmentVariable(BuilderEnvironmentVariables.RequestId, DebugRequestId);

            if (requestId.Equals(DebugRequestId, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Warning("Debug session - request id manually assigned");

                context.Subject.IsDebugSession = true;
                context.Subject.RequestId = Guid.NewGuid().ToString("N");
            }
            else
            {
                context.Subject.IsDebugSession = false;
                context.Subject.RequestId = requestId;
            }

            var fileShareEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint, context.Subject.Configuration.GetValue<string>("Endpoints:FileShareService")!);
            var salesCatalogueEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.SalesCatalogueEndpoint, context.Subject.Configuration.GetValue<string>("Endpoints:SalesCatalogueService")!);
            var buildServiceEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.BuildServiceEndpoint, context.Subject.Configuration.GetValue<string>("Endpoints:BuildService")!);

            context.Subject.FileShareEndpoint = fileShareEndpoint;
            context.Subject.SalesCatalogueEndpoint = salesCatalogueEndpoint;
            context.Subject.BuildServiceEndpoint = buildServiceEndpoint;

            Log.Information($"Request id      : {requestId}");
            Log.Information($"File Share      : {fileShareEndpoint}");
            Log.Information($"Sales Catalogue : {salesCatalogueEndpoint}");
            Log.Information($"Build Service   : {buildServiceEndpoint}");

            return Task.FromResult(NodeResultStatus.Succeeded);
        }

        private static string GetEnvironmentVariable(string variable, string overrideValue)
        {
            var value = Environment.GetEnvironmentVariable(variable);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (string.IsNullOrEmpty(overrideValue))
            {
                Log.Error($"{variable} is not set");
                throw new InvalidOperationException($"{variable} is not set");
            }

            return overrideValue;
        }
    }
}

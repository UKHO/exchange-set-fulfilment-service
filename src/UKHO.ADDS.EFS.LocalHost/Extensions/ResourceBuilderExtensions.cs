﻿using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.EFS.LocalHost.Extensions
{
    internal static class ResourceBuilderExtensions
    {
        internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
        {
            return builder.WithOpenApiDocs("Scalar API Documentation", "scalar/v1", "scalar-docs");
        }

        internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> builder, string displayName) where T : IResourceWithEndpoints
        {
            return builder.WithOpenApiDocs(displayName, "scalar/v1", "scalar-docs");
        }

        internal static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder, string displayName, string openApiUiPath, string name)
            where T : IResourceWithEndpoints
        {
            return builder.WithCommand(
                name,
                displayName,
                executeCommand: async _ =>
                {
                    try
                    {
                        var endpoint = builder.GetEndpoint("https");
                        var url = $"{endpoint.Url}/{openApiUiPath}";

                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                        return await Task.FromResult(new ExecuteCommandResult { Success = true });
                    }
                    catch (Exception e)
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = e.ToString() };

                    }
                }, updateState: context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ?
                    ResourceCommandState.Enabled : ResourceCommandState.Disabled,
                iconName: "Document", iconVariant: IconVariant.Filled);
        }

        internal static IResourceBuilder<T> WithOrchestratorDashboard<T>(this IResourceBuilder<T> builder, string displayName) where T : IResourceWithEndpoints
        {
            return builder.WithOrchestratorDashboard(displayName, "builder-dashboard");
        }

        internal static IResourceBuilder<T> WithOrchestratorDashboard<T>(this IResourceBuilder<T> builder, string displayName, string name)
            where T : IResourceWithEndpoints
        {
            return builder.WithCommand(
                name,
                displayName,
                executeCommand: async _ =>
                {
                    try
                    {
                        var endpoint = builder.GetEndpoint("https");
                        var url = $"{endpoint.Url}";

                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                        return await Task.FromResult(new ExecuteCommandResult { Success = true });
                    }
                    catch (Exception e)
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = e.ToString() };

                    }
                }, updateState: context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ?
                    ResourceCommandState.Enabled : ResourceCommandState.Disabled,
                iconName: "Document", iconVariant: IconVariant.Filled);
        }
    }
}

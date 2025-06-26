using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.Configuration.Aspire.Extensions
{
    internal static class ResourceBuilderExtensions
    {
        internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> builder, string displayName) where T : IResourceWithEndpoints => builder.WithOpenApiDocs(displayName, "scalar/v1", "scalar-docs");
        
        internal static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder, string displayName, string openApiUiPath, string name)
            where T : IResourceWithEndpoints =>
            builder.WithCommand(
                name,
                displayName,
                executeCommand: async (ExecuteCommandContext context) =>
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
                },
                commandOptions: new CommandOptions
                {
                    UpdateState = (UpdateCommandStateContext context) =>
                    {
                        // State update logic here
                        return context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled;
                    },
                    IconName = "Document",
                    IconVariant = IconVariant.Filled
                });

        internal static IResourceBuilder<T> WithDashboard<T>(this IResourceBuilder<T> builder, string displayName) where T : IResourceWithEndpoints => builder.WithDashboard(displayName, "adds-mock-dashboard");

        internal static IResourceBuilder<T> WithDashboard<T>(this IResourceBuilder<T> builder, string displayName, string name)
            where T : IResourceWithEndpoints =>
            builder.WithCommand(
                name,
                displayName,
                async context =>
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
                },
                new CommandOptions
                {
                    UpdateState = context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled, IconName = "Document", IconVariant = IconVariant.Filled
                });
    }
}

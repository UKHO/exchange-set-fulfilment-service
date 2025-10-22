using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.EFS.LocalHost.Extensions
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

        /// <summary>
        /// Only add a parameter in publish mode.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        internal static IResourceBuilder<ParameterResource>? AddPublishOnlyParameter(this IDistributedApplicationBuilder builder, string name, bool secret = false)
        {
            return builder.ExecutionContext.IsPublishMode ? builder.AddParameter(name, secret) : null;
        }

        /// <summary>
        /// Only call PublishAsExisting if the parameter is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="nameParameter"></param>
        /// <param name="resourceGroupParameter"></param>
        /// <returns></returns>
        internal static IResourceBuilder<T> PublishAsExistingWithNullCheck<T>(this IResourceBuilder<T> builder, IResourceBuilder<ParameterResource>? nameParameter, IResourceBuilder<ParameterResource>? resourceGroupParameter) where T : IAzureResource
        {
            if (nameParameter is not null)
            {
                builder.PublishAsExisting(nameParameter, resourceGroupParameter);
            }

            return builder;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.Orchestrator.Configuration;

namespace UKHO.ADDS.EFS.Orchestrator.Api.Metadata
{
    internal static class MetadataExtensions
    {
        public static RouteHandlerBuilder WithRequiredHeader(this RouteHandlerBuilder builder, string name, string description, string defaultValue)
        {
            builder.Add(x => { x.Metadata.Add(new OpenApiHeaderParameter { Name = name, Description = description, Required = true, ExpectedValue = defaultValue }); });

            return builder;
        }

        /// <summary>
        /// Requires authorization for non-local and non-development environments.
        /// Authentication is compulsory for all environments except dev and local.
        /// </summary>
        /// <param name="builder">The route handler builder</param>
        /// <param name="policyName">The authorization policy name to require</param>
        /// <returns>The route handler builder for chaining</returns>
        public static RouteHandlerBuilder RequireAuthorizationInNonLocalEnvironments(this RouteHandlerBuilder builder, string policyName)
        {
            var addsEnvironment = AddsEnvironment.GetEnvironment();

            // Authorization is compulsory for all environments except local and dev
            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                builder.RequireAuthorization(policyName);
            }

            return builder;
        }

    }
}

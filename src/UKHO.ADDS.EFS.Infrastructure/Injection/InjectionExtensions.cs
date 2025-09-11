using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Infrastructure.Services;
using UKHO.ADDS.EFS.Infrastructure.Storage.Queues;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S100;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S57;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S63;

namespace UKHO.ADDS.EFS.Infrastructure.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection collection)
        {
            var addsEnvironment = AddsEnvironment.GetEnvironment();

            collection.AddSingleton<IRepository<S100Build>, S100BuildRepository>();
            collection.AddSingleton<IRepository<S63Build>, S63BuildRepository>();
            collection.AddSingleton<IRepository<S57Build>, S57BuildRepository>();
            
            collection.AddSingleton<IRepository<DataStandardTimestamp>, DataStandardTimestampRepository>();
            collection.AddSingleton<IRepository<Job>, JobRepository>();
            collection.AddSingleton<IRepository<BuildMemento>, BuildMementoRepository>();

            collection.AddTransient<IFileNameGeneratorService, TemplateFileNameGeneratorService>();

            collection.AddSingleton<IQueueFactory, AzureQueueFactory>();

            collection.AddSingleton<IHashingService, DefaultHashingService>();
            collection.AddSingleton<IStorageService, DefaultStorageService>();
            collection.AddSingleton<ITimestampService, DefaultTimestampService>();

            // Configure Azure AD settings from configuration
            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                collection.AddAuthentication("AzureAd")
                    .AddJwtBearer("AzureAd", options =>
                    {
                        var clientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegClientId);
                        var tenantId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegTenantId);

                        options.Audience = clientId;
                        options.Authority = $"https://login.microsoftonline.com/{tenantId}";
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidAudiences = [clientId],
                            ValidIssuers = [$"https://login.microsoftonline.com/{tenantId}"]
                        };
                        options.Events = new JwtBearerEvents
                        {
                            OnForbidden = context =>
                            {
                                //context.Response.Headers.Append("origin", "JOBAPI");
                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = context =>
                            {
                                //context.Response.Headers.Append("origin", "JOBAPI");
                                return Task.CompletedTask;
                            }
                        };
                    });
            }

            collection.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("AzureAd")
                    .Build();

                options.AddPolicy("ExchangeSetFulfilmentServiceUser", policy =>
                {
                    if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                    {
                        // For non-dev/non-local environments, authentication is compulsory
                        policy.RequireRole("ExchangeSetFulfilmentServiceUser");
                    }
                    else
                    {
                        // For local and dev environments only, allow anonymous access
                        policy.RequireAssertion(_ => true);
                    }
                });
            });
            // End Azure AD settings configuration

            var efsClientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsClientId);

            collection.RegisterKiotaClient<KiotaSalesCatalogueService>(provider =>
            {
                var registry = provider.GetRequiredService<IExternalServiceRegistry>();
                var scsEndpoint = registry.GetServiceEndpoint(ProcessNames.SalesCatalogueService);

                if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                {
                    return (scsEndpoint.Uri, new AnonymousAuthenticationProvider());
                }

                return (scsEndpoint.Uri, new AzureIdentityAuthenticationProvider(new ManagedIdentityCredential(clientId: efsClientId), scopes: scsEndpoint.GetDefaultScope()));
            });

            collection.AddSingleton<IProductService, DefaultProductService>();

            collection.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            collection.AddSingleton(sp =>
            {
                var registry = sp.GetRequiredService<IExternalServiceRegistry>();
                var fssEndpoint = registry.GetServiceEndpoint(ProcessNames.FileShareService);

                IAuthenticationTokenProvider? tokenProvider = null;

                if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                {
                    tokenProvider = new AnonymousAuthenticationTokenProvider();
                }
                else
                {
                    tokenProvider = new TokenCredentialAuthenticationTokenProvider(new ManagedIdentityCredential(clientId: efsClientId), [fssEndpoint.GetDefaultScope()]);
                }

                var factory = sp.GetRequiredService<IFileShareReadWriteClientFactory>();
                return factory.CreateClient(fssEndpoint.Uri!.ToString(), tokenProvider);
            });

            collection.AddSingleton<IFileService, DefaultFileService>();

            return collection;
        }
    }
}

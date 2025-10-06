using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

            // Configure authentication
            collection.AddAuthentication(addsEnvironment);

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
            collection.AddSingleton<ICallbackNotificationService, CallbackNotificationService>();

            return collection;
        }

        private static IServiceCollection AddAuthentication(this IServiceCollection collection, AddsEnvironment addsEnvironment)
        {
            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                var (azureAdClientId, azureAdTenantId) = GetAzureAdCredentials();
                var (b2cClientId, b2cDomain, b2cInstance, b2cPolicy) = GetAzureB2CCredentials();

                collection
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = null;
                    options.DefaultChallengeScheme = null;
                })
                .AddJwtBearer(AuthenticationConstants.AzureAdScheme, options =>
                {
                    var authority = $"{AuthenticationConstants.MicrosoftLoginUrl}{azureAdTenantId}";
                    var issuer = $"{AuthenticationConstants.MicrosoftLoginUrl}{azureAdTenantId}";

                    options.Audience = azureAdClientId;
                    options.Authority = authority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudiences = [azureAdClientId],
                        ValidIssuers = [issuer]
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnForbidden = context =>
                        {
                            SetOriginHeaderIfNotExists(context.Response);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            SetOriginHeaderIfNotExists(context.Response);
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddJwtBearer(AuthenticationConstants.AzureB2CScheme, options =>
                {
                    var b2cIssuer = $"{b2cInstance}{b2cDomain}/v2.0/";
                    var b2cAuthority = $"{b2cInstance}{b2cDomain}/{b2cPolicy}/v2.0/";

                    options.Audience = b2cClientId;
                    options.Authority = b2cAuthority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudiences = [b2cClientId],
                        ValidIssuers = [b2cIssuer, b2cAuthority]
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = 401;
                                SetOriginHeaderIfNotExists(context.Response);
                                context.HandleResponse();
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            SetOriginHeaderIfNotExists(context.Response);
                            return Task.CompletedTask;
                        }
                    };
                });
            }

            // Build composite policy: B2C => authenticated only, AD => must hold role
            collection.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(AuthenticationConstants.AzureAdScheme, AuthenticationConstants.AzureB2CScheme)
                    .Build())
                .AddPolicy(AuthenticationConstants.AzureAdScheme, policy =>
                {
                    if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                    {
                        policy.RequireRole(AuthenticationConstants.EfsRole).AddAuthenticationSchemes(AuthenticationConstants.AzureAdScheme);
                    }
                    else
                    {
                        //For local and dev environments only, allow anonymous access
                        policy.RequireAssertion(_ => true);
                    }
                })
               .AddPolicy(AuthenticationConstants.AdOrB2C, policy =>
               {
                   if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                   {
                       policy.AddAuthenticationSchemes(AuthenticationConstants.AzureAdScheme, AuthenticationConstants.AzureB2CScheme);
                       policy.RequireAssertion(context =>
                       {
                           // Check if authenticated with Azure AD and has the required role
                           if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                           {
                               return false;
                           }

                           var issuer = context.User.FindFirst("iss")?.Value;

                           var (adTenantId, b2cTenantId, b2cInstance) = GetAuthorizationPolicyVariables();

                           if (!string.IsNullOrEmpty(issuer) && !string.IsNullOrEmpty(adTenantId))
                           {
                               var adAuthenticated = issuer.Contains(adTenantId, StringComparison.OrdinalIgnoreCase) &&
                                                   context.User.IsInRole(AuthenticationConstants.EfsRole);

                               if (adAuthenticated)
                               {
                                   return true;
                               }
                           }

                           // Check Azure B2C authentication (no role required)
                           if (!string.IsNullOrEmpty(b2cTenantId) && !string.IsNullOrEmpty(b2cInstance))
                           {
                               var b2cAuthenticated = issuer != null &&
                                                    issuer.Contains(b2cInstance, StringComparison.OrdinalIgnoreCase) &&
                                                    issuer.Contains(b2cTenantId, StringComparison.OrdinalIgnoreCase);
                               return b2cAuthenticated;
                           }

                           return false;
                       });
                   }
                   else
                   {
                       // For local and dev environments only, allow anonymous access
                       policy.RequireAssertion(_ => true);
                   }
               });

            return collection;
        }

        /// <summary>
        /// Retrieves Azure AD credentials from environment variables
        /// </summary>
        private static (string clientId, string tenantId) GetAzureAdCredentials()
        {
            var clientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegClientId);
            var tenantId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegTenantId);
            return (clientId, tenantId);
        }

        /// <summary>
        /// Retrieves Azure B2C credentials from environment variables
        /// </summary>
        private static (string clientId, string domain, string instance, string policy) GetAzureB2CCredentials()
        {
            var clientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppClientId);
            var domain = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppDomain);
            var instance = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance);
            var policy = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppSignInPolicy);

            return (clientId, domain, instance, policy);
        }

        /// <summary>
        /// Retrieves environment variables needed for authorization policies
        /// </summary>
        private static (string adTenantId, string b2cTenantId, string b2cInstance) GetAuthorizationPolicyVariables()
        {
            var adTenantId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegTenantId);
            var b2cTenantId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppTenantId);
            var b2cInstance = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance);

            return (adTenantId, b2cTenantId, b2cInstance);
        }

        /// <summary>
        /// Sets the origin header if it doesn't already exist to prevent duplicate headers
        /// </summary>
        /// <param name="response">The HTTP response</param>
        private static void SetOriginHeaderIfNotExists(HttpResponse response)
        {
            if (!response.Headers.ContainsKey(AuthenticationConstants.OriginHeaderKey))
            {
                response.Headers.Append(AuthenticationConstants.OriginHeaderKey, AuthenticationConstants.EfsService);
            }
        }
    }
}

using System.Security.Claims;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
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

                var authBuilder = collection
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = null;
                    options.DefaultChallengeScheme = null;
                });

                authBuilder.AddMicrosoftIdentityWebApi(
                    configureJwtBearerOptions: options =>
                    {
                        options.TokenValidationParameters.ValidAudiences = new[]
                        {
                        azureAdClientId
                        };
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = context =>
                            {
                                var claimsPrincipal = context.Principal;
                                if (!HasRole(claimsPrincipal!))
                                {
                                    context.Response.StatusCode = 403;
                                    context.Success();
                                }
                                return Task.CompletedTask;
                            },
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
                    },
                    configureMicrosoftIdentityOptions: options =>
                    {
                        options.Instance = AuthenticationConstants.MicrosoftLoginUrl;
                        options.TenantId = azureAdTenantId;
                        options.ClientId = azureAdClientId;
                    },
                    jwtBearerScheme: AuthenticationConstants.AzureAdScheme);

                authBuilder.AddMicrosoftIdentityWebApi(
                    configureJwtBearerOptions: options =>
                    {
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
                    },
                    configureMicrosoftIdentityOptions: options =>
                    {
                        options.Instance = b2cInstance;
                        options.TenantId = b2cDomain;
                        options.ClientId = b2cClientId;
                        options.Domain = b2cDomain;
                        options.SignUpSignInPolicyId = b2cPolicy;
                    },
                    jwtBearerScheme: AuthenticationConstants.AzureB2CScheme);
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
                       var (adTenantId, b2cTenantId, b2cInstance) = GetAuthorizationPolicyVariables();

                       policy.AddAuthenticationSchemes(AuthenticationConstants.AzureAdScheme, AuthenticationConstants.AzureB2CScheme);
                       policy.RequireAssertion(context =>
                       {
                           // Check if authenticated with Azure AD and has the required role
                           if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                           {
                               return false;
                           }

                           var issuer = context.User.FindFirst("iss")?.Value;

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

        /// <summary>
        /// Checks if the ClaimsPrincipal has any role (ClaimTypes.Role).
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal to check.</param>
        /// <returns>True if any role claim exists, false otherwise.</returns>
        private static bool HasRole(ClaimsPrincipal principal)
        {
            if (principal == null)
                return false;
            return principal.Claims.Any(c => c.Type == ClaimTypes.Role);
        }
    }
}

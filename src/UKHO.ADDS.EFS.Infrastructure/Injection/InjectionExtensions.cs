using System.Security.Claims;
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

            return collection;
        }

        private static IServiceCollection AddAuthentication(this IServiceCollection collection, AddsEnvironment addsEnvironment)
        {
            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                collection
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = null;
                    options.DefaultChallengeScheme = null;
                })
                .AddJwtBearer(AuthenticationConstants.AzureAdScheme, options =>
                {
                    var clientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegClientId);
                    var tenantId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegTenantId);

                    options.Audience = clientId;
                    options.Authority = $"{AuthenticationConstants.MicrosoftLoginUrl}{tenantId}";
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudiences = [clientId],
                        ValidIssuers = [$"{AuthenticationConstants.MicrosoftLoginUrl}{tenantId}"]
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
                    var b2cClientId = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppClientId);
                    var b2cDomain = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppDomain);
                    var b2cInstance = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance);
                    var b2cPolicy = Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppSignUpSignInPolicy);

                    options.Audience = b2cClientId;
                    var b2cAuthority = $"{b2cInstance}{b2cDomain}/{b2cPolicy}/v2.0/";
                    options.Authority = b2cAuthority;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudiences = [b2cClientId],
                        ValidIssuers = [b2cAuthority, $"{b2cInstance}{b2cDomain}/v2.0/"]
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
               .AddPolicy("AdOrB2C", policy =>
               {
                   if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                   {
                       policy.AddAuthenticationSchemes(AuthenticationConstants.AzureAdScheme, AuthenticationConstants.AzureB2CScheme);
                       policy.RequireAssertion(context =>
                       {
                           // Check if authenticated with Azure AD and has the required role

                           var issuer = context.User.FindFirst("iss")?.Value;

                           var adAuthenticated = context.User.Identity != null
                               && context.User.Identity.IsAuthenticated
                               && issuer!.Contains(Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsAppRegTenantId)!, StringComparison.OrdinalIgnoreCase)
                               && context.User.IsInRole(AuthenticationConstants.EfsRole);

                           // Check if authenticated with Azure B2C (no role required)
                           var b2cAuthenticated = context.User.Identity != null
                               && context.User.Identity.IsAuthenticated
                               && context.User.HasClaim("iss",
                               $"{Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance)}{Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppDomain)}/v2.0/");

                           return adAuthenticated || b2cAuthenticated;
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

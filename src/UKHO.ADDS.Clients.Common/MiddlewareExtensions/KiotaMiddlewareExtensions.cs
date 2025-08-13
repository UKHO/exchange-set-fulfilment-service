using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    /// <summary>
    /// Extension methods for registering Kiota handlers, client factory, and authentication provider in the service collection.
    /// </summary>
    public static class KiotaServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the default Kiota handlers, client factory, and authentication provider in the service collection.
        /// </summary>
        /// <typeparam name="T">The type of authentication provider to use with Kiota clients.</typeparam>
        /// <param name="services">The service collection to register Kiota services with.</param>
        /// <param name="authProvider">The authentication provider to use for client creation.</param>
        public static void AddKiotaDefaults<T>(this IServiceCollection services, T authProvider) where T : IAuthenticationProvider
        {
            services.AddSingleton<ClientFactory>();
            services.AddSingleton<IAuthenticationProvider>(authProvider);
        }

        /// <summary>
        /// Registers a Kiota client in the service collection, including its configured HTTP client and factory.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the client with.</param>
        /// <param name="endpointConfigKey">The configuration key for the endpoint URL.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        public static void RegisterKiotaClient<TClient>(
            this IServiceCollection services,
            string endpointConfigKey,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            // Ensure Inspection Handler is configured to inspect response headers
            var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
            services.AddSingleton(headersOption);
            services.AddConfiguredHttpClient<TClient>(endpointConfigKey, headers);
            services.AddSingleton(sp => sp.GetRequiredService<ClientFactory>().GetClient<TClient>());
        }

        /// <summary>
        /// Registers a Kiota client in the service collection, including its configured HTTP client and factory.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the client with.</param>
        /// <param name="uriFunc">A callback to retrieve the service URL.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        public static void RegisterKiotaClient<TClient>(
            this IServiceCollection services,
            Func<IServiceProvider, Uri> uriFunc,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            // Ensure Inspection Handler is configured to inspect response headers
            var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
            services.AddSingleton(headersOption);
            services.AddConfiguredHttpClient<TClient>(uriFunc, headers);
            services.AddSingleton(sp => sp.GetRequiredService<ClientFactory>().GetClient<TClient>());
        }

        /// <summary>
        /// Registers a Kiota client in the service collection, including its configured HTTP client and factory.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the client with.</param>
        /// <param name="endpointFunc">A callback to retrieve the service URL and auth provider.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        public static void RegisterKiotaClient<TClient>(
            this IServiceCollection services,
            Func<IServiceProvider, (Uri Uri, IAuthenticationProvider AuthenticationProvider)> endpointFunc,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
            services.AddSingleton(headersOption);

            // Reuse existing HttpClient setup by projecting to the Uri
            services.AddConfiguredHttpClient<TClient>(sp => endpointFunc(sp).Uri, headers);

            // Create the Kiota client per resolution with its own auth provider + base URL
            services.AddTransient<TClient>(sp =>
            {
                var (baseUri, authProvider) = endpointFunc(sp);

                var adapter = new HttpClientRequestAdapter(authProvider)
                {
                    BaseUrl = baseUri.AbsoluteUri.TrimEnd('/')
                };

                // Let DI satisfy any other ctor dependencies, pass IRequestAdapter explicitly.
                return ActivatorUtilities.CreateInstance<TClient>(sp, adapter);
            });
        }

        /// <summary>
        /// Attaches all registered Kiota middleware handlers to the HTTP client builder.
        /// </summary>
        /// <param name="builder">The HTTP client builder to attach handlers to.</param>
        /// <returns>The updated HTTP client builder.</returns>
        private static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
        {
            var kiotaHandlers = KiotaClientFactory.CreateDefaultHandlers([new HeadersInspectionHandlerOption() { InspectResponseHeaders = true }]);
            foreach (var handler in kiotaHandlers)
            {
                builder.AddHttpMessageHandler(() => handler);
            }

            Console.WriteLine(builder.GetType().Name + " has been configured with Kiota handlers.");
            return builder;
        }

        /// <summary>
        /// Registers and configures an HTTP client for a specific Kiota client type using a configuration key for the endpoint.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the HTTP client with.</param>
        /// <param name="endpointConfigKey">The configuration key for the endpoint URL.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        private static IHttpClientBuilder AddConfiguredHttpClient<TClient>(
            this IServiceCollection services,
            string endpointConfigKey,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            return services.AddHttpClient<TClient>((provider, client) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var endpoint = config[endpointConfigKey]!;
                client.BaseAddress = new Uri(endpoint);

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var logger = provider.GetRequiredService<ILogger<TClient>>();
                logger.LogInformation("Configured HTTP client for " + typeof(TClient).Name + " with base address: " + endpoint);
            }).AttachKiotaHandlers();
        }

        /// <summary>
        /// Registers and configures an HTTP client for a specific Kiota client type using a configuration key for the endpoint.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the HTTP client with.</param>
        /// <param name="uri">The service URI</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        private static IHttpClientBuilder AddConfiguredHttpClient<TClient>(
            this IServiceCollection services,
            Func<IServiceProvider, Uri> uriFunc,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            return services.AddHttpClient<TClient>((provider, client) =>
            {
                var uri = uriFunc(provider);

                client.BaseAddress = uri;

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var logger = provider.GetRequiredService<ILogger<TClient>>();
                logger.LogInformation("Configured HTTP client for " + typeof(TClient).Name + " with base address: " + uri.AbsoluteUri);
            }).AttachKiotaHandlers();
        }
    }
}

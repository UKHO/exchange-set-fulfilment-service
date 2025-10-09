using Microsoft.AspNetCore.WebUtilities;
using UKHO.ADDS.EFS.FunctionalTests.Configuration;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;

namespace UKHO.ADDS.EFS.FunctionalTests.Utilities
{
    /// <summary>
    /// Provides utility methods for building API endpoint URLs with query parameters
    /// </summary>
    public static class EndpointUtility
    {
        /// <summary>
        /// Builds an API endpoint with appropriate callback and product identifier parameters
        /// </summary>
        /// <param name="baseEndpoint">The base endpoint without query parameters</param>
        /// <param name="callbackUri">Optional callback URI</param>
        /// <param name="productIdentifier">Optional product identifier</param>
        /// <param name="assertCallbackTextFile">Output flag indicating if callback txt file should be asserted</param>
        /// <returns>The complete endpoint URL with query parameters</returns>
        public static string BuildEndpoint(
            string baseEndpoint, 
            string? callbackUri, 
            string? productIdentifier,
            out bool assertCallbackTextFile)
        {
            assertCallbackTextFile = false;
            var queryParams = new Dictionary<string, string>();

            // Handle callback URI logic
            if (callbackUri != null)
            {
                string callbackUrlToUse = callbackUri;
                
                // Special handling for the valid test callback URL
                if (string.Equals(callbackUri, TestEndpointConfiguration.ValidCallbackUrl, StringComparison.OrdinalIgnoreCase))
                {
                    assertCallbackTextFile = true;
                    
                    // Get the base URL from the HttpClient - with null safety
                    var httpClient = AspireTestHost.httpClientMock;
                    if (httpClient?.BaseAddress != null)
                    {
                        var baseUrl = httpClient.BaseAddress.ToString();
                        callbackUrlToUse = baseUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase)
                            ? TestEndpointConfiguration.LocalhostCallbackMockUrl
                            : TestEndpointConfiguration.AzureCallbackMockUrl;
                    }
                }
                
                queryParams.Add("callbackUri", callbackUrlToUse);
            }

            // Add product identifier if provided
            if (productIdentifier != null)
            {
                queryParams.Add("productIdentifier", productIdentifier);
            }

            // Use WebUtilities to properly build the URL with query parameters
            return queryParams.Count > 0 
                ? QueryHelpers.AddQueryString(baseEndpoint, queryParams!)
                : baseEndpoint;
        }
    }
}

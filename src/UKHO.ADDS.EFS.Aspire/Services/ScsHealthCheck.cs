using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.EFS.Aspire.Services
{
    public class ScsHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ScsHealthCheck(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            request.Headers.Add("If-Modified-Since", DateTime.Parse("2021-03-15T14:47:00").ToString("R"));
            // Use your actual value
            try
            {
                var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("SCS endpoint is healthy");
                }
                return HealthCheckResult.Unhealthy("SCS endpoint is unhealthy");
            }
            catch
            {
                return HealthCheckResult.Unhealthy("SCS endpoint is unreachable");
            }
        }
    }
    }

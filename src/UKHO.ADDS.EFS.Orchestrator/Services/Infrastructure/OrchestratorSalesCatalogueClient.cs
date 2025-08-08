using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.V2;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;
using ProductCounts = UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models.ProductCounts;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    internal class OrchestratorSalesCatalogueClient : IOrchestratorSalesCatalogueClient
    {
        private readonly KiotaSalesCatalogueService _kiotaClient;
        private readonly ILogger<OrchestratorSalesCatalogueClient> _logger;

        public OrchestratorSalesCatalogueClient(
            KiotaSalesCatalogueService kiotaClient,
            ILogger<OrchestratorSalesCatalogueClient> logger)
        {
            _kiotaClient = kiotaClient ?? throw new ArgumentNullException(nameof(kiotaClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Fix for CS1061: Replace the incorrect usage of 'IsSuccess' with proper null checks and error handling.

        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, Job job)
        {
            try
            {
                var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
                var headerDateString = sinceDateTime?.ToString("R");
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
                var s100BasicCatalogueResult =
                await _kiotaClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                {
                    config.Headers.Add("If-Modified-Since", headerDateString);
                    config.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                    config.Options.Add(headersOption);
                });

                var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                if (s100BasicCatalogueResult != null)
                {
                    var response = new S100SalesCatalogueResponse
                    {
                        ResponseBody = s100BasicCatalogueResult.Select(x =>
                        {
                            S100ProductStatus? parsedStatus = null;
                            if (x.Status is not null && Enum.TryParse(typeof(S100BasicCatalogue_status_statusName), x.Status.ToString(), out var tempStatusObj))
                            {
                                parsedStatus = tempStatusObj as S100ProductStatus;
                            }
                            return new S100Products
                            {
                                ProductName = x.ProductName,
                                LatestEditionNumber = x.LatestEditionNumber,
                                LatestUpdateNumber = x.LatestUpdateNumber,
                                Status = parsedStatus
                            };
                        }).ToList() ?? new List<S100Products>(),
                        LastModified = sinceDateTime,
                        ResponseCode = HttpStatusCode.OK
                    };

                    switch (response.ResponseCode)
                    {
                        case HttpStatusCode.OK:
                            return (response, response.LastModified);
                        case HttpStatusCode.NotModified:
                            return (response, response.LastModified);
                        default:
                            _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, response.ResponseCode));
                            return (new S100SalesCatalogueResponse(), sinceDateTime);
                    }
                }

                // API call failed, log the error
                _logger.LogSalesCatalogueApiError(null, SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }
            catch (Exception ex)
            {
                _logger.LogSalesCatalogueApiError(null, SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }
        }

        public async Task<S100ProductNamesResponse> GetS100ProductNamesAsync(IEnumerable<string> productNames, Job job, CancellationToken cancellationToken)
        {
            try
            {
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse>(_logger, nameof(GetS100ProductNamesAsync));
                var s100ProductResponseResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await _kiotaClient.V2
                        .Products
                        .S100
                        .ProductNames
                        .PostAsync(productNames.ToList(), requestConfiguration =>
                        {
                            requestConfiguration.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                        }, cancellationToken);

                    if (response is S100ProductResponse s100ProductResponse)
                    {
                        // Manual mapping for ProductCounts
                        UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts? mappedProductCounts = null;
                        if (s100ProductResponse.ProductCounts != null)
                        {
                            mappedProductCounts = new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts
                            {
                                RequestedProductCount = s100ProductResponse.ProductCounts.RequestedProductCount,
                                ReturnedProductCount = s100ProductResponse.ProductCounts.ReturnedProductCount,
                                RequestedProductsAlreadyUpToDateCount = s100ProductResponse.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                                RequestedProductsNotReturned = s100ProductResponse.ProductCounts.RequestedProductsNotReturned?
                                    .Select(x => new UKHO.ADDS.Clients.SalesCatalogueService.Models.RequestedProductsNotReturned
                                    {
                                        ProductName = x.ProductName,
                                        //Reason = x.Reason
                                        // Map other properties if needed
                                    }).ToList()
                            };
                        }

                        // JSON mapping for Products
                        string productsJson = JsonSerializer.Serialize(s100ProductResponse.Products);
                        var mappedProducts = JsonSerializer.Deserialize<List<S100ProductNames>>(productsJson);

                        var result = new S100ProductNamesResponse
                        {
                            ProductCounts = mappedProductCounts,
                            Products = mappedProducts ?? new List<S100ProductNames>()
                        };

                        // Return as S100ProductResponse for retry policy
                        return Result.Success(s100ProductResponse);
                    }
                    else
                    {
                        return Result.Failure<S100ProductResponse>(new Error("Null or invalid response from Sales Catalogue Service."));
                    }
                });

                if (s100ProductResponseResult.IsSuccess(out var s100ProductResponse, out var error))
                {
                    // Manual mapping for ProductCounts
                    UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts? mappedProductCounts = null;
                    if (s100ProductResponse.ProductCounts != null)
                    {
                        mappedProductCounts = new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts
                        {
                            RequestedProductCount = s100ProductResponse.ProductCounts.RequestedProductCount,
                            ReturnedProductCount = s100ProductResponse.ProductCounts.ReturnedProductCount,
                            RequestedProductsAlreadyUpToDateCount = s100ProductResponse.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                            RequestedProductsNotReturned = s100ProductResponse.ProductCounts.RequestedProductsNotReturned?
                                .Select(x => new UKHO.ADDS.Clients.SalesCatalogueService.Models.RequestedProductsNotReturned
                                {
                                    ProductName = x.ProductName,
                                    // Reason = x.Reason
                                    // Map other properties if needed
                                }).ToList()
                        };
                    }

                    // JSON mapping for Products
                    string productsJson = JsonSerializer.Serialize(s100ProductResponse.Products);
                    var mappedProducts = JsonSerializer.Deserialize<List<S100ProductNames>>(productsJson);

                    var result = new S100ProductNamesResponse
                    {
                        ProductCounts = mappedProductCounts,
                        Products = mappedProducts ?? new List<S100ProductNames>()
                    };

                    return result;
                }

                _logger.LogSalesCatalogueApiError(error, SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
            catch (Exception ex)
            {
                return new S100ProductNamesResponse();
            }
        }

        public static class JsonResponseHelper
        {
            public static string ToJson<T>(T response)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.Serialize(response, options);
            }
        }
    }
}

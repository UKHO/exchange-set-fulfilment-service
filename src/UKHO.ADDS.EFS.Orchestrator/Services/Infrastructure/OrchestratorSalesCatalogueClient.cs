using System.Net;
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

        public static async Task<IResult<T>> KiotaRunnerAsync<T>(Func<Task<T>> kiotaTask, string correlationId)
        {
            try
            {
                var result = await kiotaTask();
                return Result.Success(result);
            }
            catch (ApiException ex)
            {
                var correlationIdProperty = ex.GetType().GetProperty("correlationId");
                if (correlationIdProperty != null)
                {
                    correlationId = correlationIdProperty.GetValue(ex)?.ToString();
                }

                if ((HttpStatusCode)ex.ResponseStatusCode == HttpStatusCode.NotModified)
                {
                    // If the response is NotModified, we can return a success result with null data
                    return Result.Success<T>(default);
                }

                return Result.Failure<T>(ErrorFactory.CreateError(
                    (HttpStatusCode)ex.ResponseStatusCode,
                    ex.Message,
                    ErrorFactory.CreateProperties(correlationId)));
            }
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
                            if (x.Status is not null && Enum.TryParse(typeof(S100ProductStatus), x.Status.ToString(), out var tempStatusObj))
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
                var response = await _kiotaClient.V2
                    .Products
                    .S100
                    .ProductNames
                    .PostAsync(productNames.ToList(), requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                    }, cancellationToken);

                return new S100ProductNamesResponse
                {
                    Products = response?.Products?.Select(x => new S100ProductNames
                    {
                        ProductName = x.ProductName,
                        EditionNumber = x.EditionNumber ?? 0,
                        UpdateNumbers = x.UpdateNumbers != null ? x.UpdateNumbers.Where(i => i.HasValue).Select(i => i.Value).ToList() : new List<int>(),
                        Dates = x.Dates?.Select(d => new S100ProductDate
                        {
                            // Map properties as needed
                        }).ToList() ?? new List<S100ProductDate>(),
                        FileSize = x.FileSize ?? 0,
                        Cancellation = x.Cancellation is null ? null : new S100ProductCancellation
                        {
                            // Map properties as needed
                        }
                    }).ToList() ?? new List<S100ProductNames>(),
                    ProductCounts = response?.ProductCounts is null ? null : new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts
                    {
                        RequestedProductCount = response.ProductCounts.RequestedProductCount,
                        ReturnedProductCount = response.ProductCounts.ReturnedProductCount,
                        RequestedProductsAlreadyUpToDateCount = response.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                        RequestedProductsNotReturned = response.ProductCounts.RequestedProductsNotReturned?.Select(r => new RequestedProductsNotReturned
                        {
                            // Map properties as needed
                        }).ToList() ?? new List<RequestedProductsNotReturned>()
                    },
                    ResponseCode = HttpStatusCode.OK
                };
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotModified)
            {
                return new S100ProductNamesResponse
                {
                    Products = new List<S100ProductNames>(),
                    ProductCounts = new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts(),
                    ResponseCode = HttpStatusCode.NotModified
                };
            }
            catch (Exception ex)
            {
                _logger.LogSalesCatalogueApiError(null, SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
        }
    }
}

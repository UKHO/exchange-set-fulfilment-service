using System.Globalization;
using System.Net;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    internal class OrchestratorSalesCatalogueClient : IOrchestratorSalesCatalogueClient
    {
        private readonly KiotaSalesCatalogueService _kiotaSalesCatalogueService;
        private readonly ILogger<OrchestratorSalesCatalogueClient> _logger;
        private const string ErrorMessage = "An Unexpected error occurred at SCS side";

        public OrchestratorSalesCatalogueClient(
            KiotaSalesCatalogueService kiotaClient,
            ILogger<OrchestratorSalesCatalogueClient> logger)
        {
            _kiotaSalesCatalogueService = kiotaClient ?? throw new ArgumentNullException(nameof(kiotaClient));
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
                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _kiotaSalesCatalogueService.V2.Catalogues.S100.Basic.GetAsync(config =>
                    {
                        config.Headers.Add("If-Modified-Since", headerDateString);
                        config.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                        config.Options.Add(headersOption);
                    });
                    return Result.Success(result);
                });

                // Fix: Use IsSuccess to check the result and retrieve the value
                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList, out var error))
                {
                    catalogueList ??= new List<S100BasicCatalogue>();

                    // Parse the "Last-Modified" header value to DateTime? if it exists
                    var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                                         ? values.FirstOrDefault()
    : null;
                    DateTime.TryParse(lastModified ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastModifiedActual);

                    if (catalogueList.Count > 0)
                    {
                        var response = new S100SalesCatalogueResponse
                        {
                            ResponseBody = catalogueList.Select(x =>
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
                            }).ToList(),
                            LastModified = lastModifiedActual,
                            ResponseCode = HttpStatusCode.OK
                        };

                        switch (response.ResponseCode)
                        {
                            case HttpStatusCode.OK:
                                return (response, response.LastModified);
                            //case HttpStatusCode.NotModified:
                            //    return (response, response.LastModified);
                            default:
                                _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, response.ResponseCode));
                                return (new S100SalesCatalogueResponse(), sinceDateTime);
                        }
                    }
                }
                // API call failed, log the error
                _logger.LogSalesCatalogueApiError(error, SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotModified)
            {
                _logger.LogSalesCatalogueApiError(new Error() { Message= ErrorMessage, Metadata =  new Dictionary<string, object>
                {
                    ["correlationId"] = job.GetCorrelationId(),
                    ["ErrorOrigin"] = "SCS",
                    ["ErrorResponse"] = ex.Message, // or ex.ResponseContent if available
                    ["StatusCode"] = ex.ResponseStatusCode.ToString()
                }
                }, SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse() { ResponseCode = HttpStatusCode.NotModified }, sinceDateTime);
            }
            catch (ApiException ex)
            {
                _logger.LogSalesCatalogueApiError(new Error()
                {
                    Message = ErrorMessage,
                    Metadata = new Dictionary<string, object>
                    {
                        ["correlationId"] = job.GetCorrelationId(),
                        ["ErrorOrigin"] = "SCS",
                        ["ErrorResponse"] = ErrorMessage, // or ex.ResponseContent if available
                        ["StatusCode"] = ex.ResponseStatusCode.ToString()
                    }
                }, SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }
        }

        public async Task<S100ProductNamesResponse> GetS100ProductNamesAsync(IEnumerable<string> productNames, Job job, CancellationToken cancellationToken)
        {
            try
            {
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetS100ProductNamesAsync));
                var responseResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _kiotaSalesCatalogueService.V2.Products.S100.ProductNames.PostAsync(productNames.ToList(), requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                    }, cancellationToken);
                    return Result.Success(result);
                });

                // Fix: Use IsSuccess to extract the response value
                if (responseResult.IsSuccess(out var response) && response != null)
                {
                    return new S100ProductNamesResponse
                    {
                        Products = response.Products?.Select(x => new S100ProductNames
                        {
                            ProductName = x.ProductName ?? string.Empty,
                            EditionNumber = x.EditionNumber ?? 0,
                            UpdateNumbers = x.UpdateNumbers != null ? x.UpdateNumbers.Where(i => i.HasValue).Select(i => i.Value).ToList() : new List<int>(),
                            Dates = x.Dates?.Select(d => new S100ProductDate
                            {
                                IssueDate = d.IssueDate.HasValue ? d.IssueDate.Value.DateTime : default,
                                UpdateApplicationDate = d.UpdateApplicationDate.HasValue ? d.UpdateApplicationDate.Value.DateTime : default,
                                UpdateNumber = d.UpdateNumber ?? 0
                            }).ToList() ?? new List<S100ProductDate>(),
                            FileSize = x.FileSize ?? 0,
                            Cancellation = x.Cancellation is null
                                ? null
                                : new S100ProductCancellation
                                {
                                    EditionNumber = x.Cancellation?.EditionNumber ?? 0,
                                    UpdateNumber = x.Cancellation?.UpdateNumber ?? 0
                                }
                        }).ToList() ?? new List<S100ProductNames>(),
                        ProductCounts = response.ProductCounts == null
                            ? null
                            : new Clients.SalesCatalogueService.Models.ProductCounts
                            {
                                RequestedProductCount = response.ProductCounts.RequestedProductCount ?? 0,
                                RequestedProductsAlreadyUpToDateCount = response.ProductCounts.RequestedProductsAlreadyUpToDateCount ?? 0,
                                RequestedProductsNotReturned = response.ProductCounts.RequestedProductsNotReturned?
                                    .Select(r => new RequestedProductsNotReturned
                                    {
                                        ProductName = r.ProductName ?? string.Empty,
                                        Reason = r.Reason?.ToString() ?? string.Empty,
                                    }).ToList() ?? new List<RequestedProductsNotReturned>()
                            },
                        ResponseCode = HttpStatusCode.OK
                    };
                }

                _logger.LogSalesCatalogueApiError(null, SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotModified)
            {
                _logger.LogSalesCatalogueApiError(new Error()
                {
                    Message = ErrorMessage,
                    Metadata = new Dictionary<string, object>
                    {
                        ["correlationId"] = job.GetCorrelationId(),
                        ["ErrorOrigin"] = "SCS",
                        ["ErrorResponse"] = ErrorMessage, // or ex.ResponseContent if available
                        ["StatusCode"] = ex.ResponseStatusCode.ToString()
                    }
                }, SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse
                {
                    Products = new List<S100ProductNames>(),
                    ProductCounts = new Clients.SalesCatalogueService.Models.ProductCounts(),
                    ResponseCode = HttpStatusCode.NotModified
                };
            }
            catch (ApiException ex)
            {
                _logger.LogSalesCatalogueApiError(new Error()
                {
                    Message = "ErrorMessage",
                    Metadata = new Dictionary<string, object>
                    {
                        ["correlationId"] = job.GetCorrelationId(),
                        ["ErrorOrigin"] = "SCS",
                        ["ErrorResponse"] = ErrorMessage, // or ex.ResponseContent if available
                        ["StatusCode"] = ex.ResponseStatusCode.ToString()
                    }
                }, SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
        }
    }
}

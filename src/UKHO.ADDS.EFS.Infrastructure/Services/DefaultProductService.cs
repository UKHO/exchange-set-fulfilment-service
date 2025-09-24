using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Adapters.Products;
using UKHO.ADDS.EFS.Infrastructure.Logging;
using UKHO.ADDS.EFS.Infrastructure.Logging.Services;
using UKHO.ADDS.EFS.Infrastructure.Retries;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    internal class DefaultProductService : IProductService
    {
        private readonly ILogger<DefaultProductService> _logger;
        private readonly KiotaSalesCatalogueService _salesCatalogueClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultProductService" /> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public DefaultProductService(KiotaSalesCatalogueService salesCatalogueClient, ILogger<DefaultProductService> logger)
        {
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(ProductList ProductList, DateTime? LastModified)> GetProductVersionListAsync(DataStandard dataStandard, DateTime? sinceDateTime, Job job)
        {
            if (dataStandard != DataStandard.S100)
            {
                throw new NotImplementedException($"Data standard {dataStandard} is not supported.");
            }

            var headersOption = new HeadersInspectionHandlerOption
            {
                InspectResponseHeaders = true
            };

            try
            {
                var headerDateString = sinceDateTime?.ToString("R");
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetProductVersionListAsync));

                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                    {
                        if (!string.IsNullOrEmpty(headerDateString))
                        {
                            config.Headers.Add("If-Modified-Since", headerDateString);
                        }

                        config.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                        config.Options.Add(headersOption);
                    });

                    return Result.Success(result);
                });

                var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModifiedHeader, out var lastModifiedActual);

                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList) && catalogueList is not null)
                {
                    var response = catalogueList.ToDomain(lastModifiedActual);
                    return (response, response.LastModified);
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return (new ProductList(), sinceDateTime);
            }
            catch (ApiException apiException)
            {
                switch (apiException.ResponseStatusCode)
                {
                    case (int)HttpStatusCode.NotModified:
                        {
                            var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                                ? values.FirstOrDefault()
                                : null;

                            if (!DateTime.TryParse(lastModifiedHeader, out var parsed))
                            {
                                // Fall back if header missing or unparsable
                                parsed = sinceDateTime ?? default;
                            }

                            return (new ProductList
                            {
                                ResponseCode = HttpStatusCode.NotModified
                            }, parsed);
                        }

                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return (new ProductList(), sinceDateTime);
                }
            }
        }

        public async Task<ProductEditionList> GetProductEditionListAsync(DataStandard dataStandard, IEnumerable<ProductName> productNames, Job job, CancellationToken cancellationToken)
        {
            if (dataStandard != DataStandard.S100)
            {
                throw new NotImplementedException($"Data standard {dataStandard} is not supported.");
            }

            try
            {
                var retryPolicy =
                    HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetProductEditionListAsync));

                var s100ProductNamesResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var payload = productNames.Select(x => (string)x).ToList();

                    var result = await _salesCatalogueClient.V2.Products.S100.ProductNames.PostAsync(payload,
                        requestConfiguration =>
                        {
                            requestConfiguration.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                        },
                        cancellationToken);

                    return Result.Success(result);
                });

                if (s100ProductNamesResult.IsSuccess(out var response) && response is not null)
                {
                    return response.ToDomain();
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return new ProductEditionList();
            }
            catch (ApiException apiException)
            {
                _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                return new ProductEditionList();
            }
        }

        public async Task<ProductEditionList> GetProductVersionsListAsync(DataStandard dataStandard, IEnumerable<ProductVersion> productVersion, Job job, CancellationToken cancellationToken)
        {
            if (dataStandard != DataStandard.S100)
            {
                throw new NotImplementedException($"Data standard {dataStandard} is not supported.");
            }

            try
            {
                var retryPolicy =
                  HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetProductVersionsListAsync));

                var payload = productVersion.Select(p => new S100ProductVersions
                {
                    ProductName = (string)p.ProductName,
                    EditionNumber = (int?)p.EditionNumber,
                    UpdateNumber = (int?)p.UpdateNumber
                }).ToList();

                var s100ProductVersionResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Products.S100.ProductVersions.PostAsync(
                        payload,
                        requestConfiguration =>
                        {
                            requestConfiguration.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                        },
                        cancellationToken);

                    return Result.Success(result);
                });

                if (s100ProductVersionResult.IsSuccess(out var response) && response is not null)
                {
                    return response.ToDomain();
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return [];
            }
            catch (ApiException apiException)
            {
                _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                return [];
            }
        }
    }
}

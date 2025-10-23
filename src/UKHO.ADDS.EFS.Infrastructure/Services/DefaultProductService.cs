using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.ExternalErrors;
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
        private const string DateTimeFormat = "R";

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

            var headersOption = CreateHeadersOption();
            try
            {
                var headerDateString = sinceDateTime?.ToString(DateTimeFormat);
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetProductVersionListAsync));

                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                    {
                        if (!string.IsNullOrEmpty(headerDateString))
                        {
                            config.Headers.Add(ApiHeaderKeys.IfModifiedSinceHeaderKey, headerDateString);
                        }

                        config.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, (string)job.GetCorrelationId());
                        config.Options.Add(headersOption);
                    });

                    return Result.Success(result);
                });

                var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue(ApiHeaderKeys.LastModifiedHeaderKey, out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModifiedHeader, CultureInfo.InvariantCulture, out var lastModifiedActual);

                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList) && catalogueList is not null)
                {
                    var response = catalogueList.ToDomain(lastModifiedActual);
                    return (response, response.ProductsLastModified);
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
                            var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue(ApiHeaderKeys.LastModifiedHeaderKey, out var values)
                                ? values.FirstOrDefault()
                                : null;

                            if (!DateTime.TryParse(lastModifiedHeader, CultureInfo.InvariantCulture, out var parsed))
                            {
                                // Fall back if header missing or unparsable
                                parsed = sinceDateTime ?? default;
                            }

                            return (new ProductList
                            {
                                ErrorResponseCode = HttpStatusCode.NotModified
                            }, parsed);
                        }

                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return ([], sinceDateTime);
                }
            }
        }

        public async Task<(ProductEditionList, ExternalServiceError?)> GetProductEditionListAsync(DataStandard dataStandard, IEnumerable<ProductName> productNames, Job job, CancellationToken cancellationToken)
        {
            if (dataStandard != DataStandard.S100)
            {
                throw new NotImplementedException($"Data standard {dataStandard} is not supported.");
            }
            var headersOption = CreateHeadersOption();
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
                            requestConfiguration.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, (string)job.GetCorrelationId());
                            requestConfiguration.Options.Add(headersOption);
                        },
                        cancellationToken);

                    return Result.Success(result);
                });

                return ProcessProductNamesResult(s100ProductNamesResult, headersOption, job);
            }
            catch (ApiException apiException)
            {
                return HandleApiExceptionForProductEditionList(apiException, headersOption, job);
            }
        }

        public async Task<(ProductEditionList, ExternalServiceError?)> GetS100ProductUpdatesSinceAsync(string sinceDateTime, DataStandardProduct productIdentifier, Job job, CancellationToken cancellationToken)
        {
            var headersOption = CreateHeadersOption();

            try
            {
                var retryPolicy =
                    HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetS100ProductUpdatesSinceAsync));

                var s100ProductUpdatesResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Products.S100.UpdatesSince
                        .GetAsync(
                            requestConfiguration =>
                            {
                                if (productIdentifier != DataStandardProduct.Undefined)
                                {
                                    requestConfiguration.QueryParameters.ProductIdentifier = productIdentifier.AsEnum.ToString();
                                }

                                requestConfiguration.QueryParameters.SinceDateTime = DateTimeOffset.Parse(sinceDateTime);
                                requestConfiguration.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, (string)job.GetCorrelationId());
                                requestConfiguration.Options.Add(headersOption);
                            },
                            cancellationToken);

                    return Result.Success(result);

                });

                return ProcessProductNamesResult(s100ProductUpdatesResult, headersOption, job);
            }
            catch (ApiException apiException)
            {
                return HandleApiExceptionForProductEditionList(apiException, headersOption, job);
            }
        }

        public async Task<(ProductEditionList, ExternalServiceError?)> GetProductVersionsListAsync(DataStandard dataStandard, ProductVersionList productVersions, Job job, CancellationToken cancellationToken)
        {
            if (dataStandard != DataStandard.S100)
            {
                throw new NotImplementedException($"Data standard {dataStandard} is not supported.");
            }
            var headersOption = CreateHeadersOption();

            try
            {
                var retryPolicy =
                  HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetProductVersionsListAsync));

                var payload = productVersions.Select(p => new S100ProductVersions
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
                            requestConfiguration.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, (string)job.GetCorrelationId());
                            requestConfiguration.Options.Add(headersOption);
                        },
                        cancellationToken);

                    return Result.Success(result);
                });

                return ProcessProductNamesResult(s100ProductVersionResult, headersOption, job);
            }
            catch (ApiException apiException)
            {
                return HandleApiExceptionForProductEditionList(apiException, headersOption, job);
            }
        }

        private (ProductEditionList, ExternalServiceError) HandleApiExceptionForProductEditionList(ApiException apiException, HeadersInspectionHandlerOption headersOption, Job job)
        {
            var productEditionList = new ProductEditionList();

            var externalServiceError = new ExternalServiceError(
                (HttpStatusCode)apiException.ResponseStatusCode,
                ExternalServiceName.SalesCatalogueService
            );

            if (apiException.ResponseStatusCode == (int)HttpStatusCode.NotModified)
            {
                var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue(ApiHeaderKeys.LastModifiedHeaderKey, out var values)
                    ? values.FirstOrDefault()
                    : null;

                if (!DateTime.TryParse(lastModifiedHeader, CultureInfo.InvariantCulture, out var parsed))
                {
                    // Fall back if header missing or unparsable
                    parsed = default;
                }

                productEditionList.ProductsLastModified = parsed;
            }

            _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
            return (productEditionList, externalServiceError);
        }

        private (ProductEditionList, ExternalServiceError?) ProcessProductNamesResult(IResult<S100ProductResponse?> s100ProductNamesResult, HeadersInspectionHandlerOption headersOption, Job job)
        {
            var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue(ApiHeaderKeys.LastModifiedHeaderKey, out var values)
                ? values.FirstOrDefault()
                : null;

            _ = DateTime.TryParse(lastModifiedHeader, CultureInfo.InvariantCulture, out var lastModifiedActual);

            if (s100ProductNamesResult.IsSuccess(out var productList) && productList is not null)
            {
                return (productList.ToDomain(lastModifiedActual), null);
            }

            _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
            return ([], null);
        }

        private static HeadersInspectionHandlerOption CreateHeadersOption()
        {
            return new HeadersInspectionHandlerOption
            {
                InspectResponseHeaders = true
            };
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class SalesCatalogueKiotaClientAdapter : ISalesCatalogueKiotaClientAdapter
    {
        private readonly KiotaSalesCatalogueService _kiotaSalesCatalogueService;

        public SalesCatalogueKiotaClientAdapter(KiotaSalesCatalogueService kiotaSalesCatalogueService)
        {
            _kiotaSalesCatalogueService = kiotaSalesCatalogueService ?? throw new ArgumentNullException(nameof(kiotaSalesCatalogueService));
        }

        public async Task<List<S100BasicCatalogue>?> GetBasicCatalogueAsync(DateTime? sinceDateTime, Job job, HeadersInspectionHandlerOption headersOption, CancellationToken cancellationToken)
        {
            var headerDateString = sinceDateTime?.ToString("R");
            return await _kiotaSalesCatalogueService.V2.Catalogues.S100.Basic.GetAsync(config =>
            {
                config.Headers.Add("If-Modified-Since", headerDateString!);
                config.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                config.Options.Add(headersOption);
            }, cancellationToken);
        }

        public async Task<S100ProductResponse?> PostProductNamesAsync(List<string> productNames, Job job, CancellationToken cancellationToken)
        {
            return await _kiotaSalesCatalogueService.V2.Products.S100.ProductNames.PostAsync(productNames, config =>
            {
                config.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
            }, cancellationToken);
        }
    }
}

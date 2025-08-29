using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    public interface ISalesCatalogueKiotaClientAdapter
    {
        Task<List<S100BasicCatalogue>?> GetBasicCatalogueAsync(DateTime? sinceDateTime, Job job, HeadersInspectionHandlerOption headersOption, CancellationToken cancellationToken);
        Task<S100ProductResponse?> PostProductNamesAsync(List<string> productNames, Job job, CancellationToken cancellationToken);
    }
}

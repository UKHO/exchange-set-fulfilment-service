using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public interface ISalesCatalogueService
    {
        Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(string apiVersion,
                string productType,
                DateTime? sinceDateTime,
                ExchangeSetRequestQueueMessage correlationId);
    }
}

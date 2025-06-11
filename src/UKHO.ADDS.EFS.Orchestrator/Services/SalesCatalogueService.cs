using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<SalesCatalogueService> _logger;

        public SalesCatalogueService(ISalesCatalogueClient salesCatalogueClient, ILogger<SalesCatalogueService> logger)
        {
            _salesCatalogueClient = salesCatalogueClient;
            _logger = logger;
        }

        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(string apiVersion,
                string productType,
                DateTime? sinceDateTime,
                ExchangeSetRequestQueueMessage message)
        {
            var s100SalesCatalogueResult = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, message.CorrelationId);

            if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            {
                switch (s100SalesCatalogueData.ResponseCode)
                {
                    case HttpStatusCode.OK:
                        return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

                    case HttpStatusCode.NotModified:
                        return (s100SalesCatalogueData, sinceDateTime);
                }
            }
            else
            {
                _logger.LogSalesCatalogueError(error, message);
            }

            return (new S100SalesCatalogueResponse(), sinceDateTime);
        }
    }
}

using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories
{
    /// <summary>
    /// Factory for creating exchange set response objects
    /// </summary>
    internal class ExchangeSetResponseFactory : IExchangeSetResponseFactory
    {
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly IConfiguration _configuration;
        private readonly IFileNameGeneratorService _fileNameGeneratorService;

        public ExchangeSetResponseFactory(
            IExternalServiceRegistry externalServiceRegistry,
            IConfiguration configuration,
            IFileNameGeneratorService fileNameGeneratorService)
        {
            _externalServiceRegistry = externalServiceRegistry;
            _configuration = configuration;
            _fileNameGeneratorService = fileNameGeneratorService;
        }

        /// <summary>
        /// Creates a custom exchange set response from job data
        /// </summary>
        /// <param name="job">The job containing exchange set information</param>
        /// <returns>A configured CustomExchangeSetResponse</returns>
        public CustomExchangeSetResponse CreateResponse(Job job)
        {
            var fssEndpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService);
            var baseUri = fssEndpoint.Uri.ToString().TrimEnd('/');
            
            // Get the exchange set name template from configuration
            var exchangeSetNameTemplate = _configuration["orchestrator:Builders:S100:ExchangeSetNameTemplate"] ?? "S100-ExchangeSet_{0}.zip";
            var fileName = _fileNameGeneratorService.GenerateFileName(exchangeSetNameTemplate, job.Id);

            // Create links using the endpoint from the registry
            var links = new ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new Link { Uri = new Uri($"{baseUri}/batch/{job.BatchId}/status") },
                ExchangeSetBatchDetailsUri = new Link { Uri = new Uri($"{baseUri}/batch/{job.BatchId}") },
                ExchangeSetFileUri = new Link { Uri = new Uri($"{baseUri}/batch/{job.BatchId}/files/{fileName}") }
            };

            return new CustomExchangeSetResponse
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = job.ExchangeSetUrlExpiryDateTime,
                RequestedProductCount = job.RequestedProductCount,
                ExchangeSetProductCount = job.ExchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = job.RequestedProductsAlreadyUpToDateCount,
                RequestedProductsNotInExchangeSet = job.RequestedProductsNotInExchangeSet,
                FssBatchId = job.BatchId
            };
        }
    }
}

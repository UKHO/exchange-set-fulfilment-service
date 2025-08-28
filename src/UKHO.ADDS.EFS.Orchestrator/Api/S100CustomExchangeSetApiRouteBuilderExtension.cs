using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    /// <summary>
    /// Extension methods for registering S100 Exchange Set API endpoints
    /// </summary>
    internal static class S100CustomExchangeSetApiRouteBuilderExtension
    {
        /// <summary>
        /// Registers S100 Exchange Set API endpoints
        /// </summary>
        /// <param name="routeBuilder">The endpoint route builder</param>
        /// <param name="loggerFactory">The logger factory</param>
        public static void RegisterS100CustomExchangeSetApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("S100ExchangeSetApi");
            var exchangeSetEndpoint = routeBuilder.MapGroup("/v2/exchangeSet/s100");

            // POST /v2/exchangeSet/s100/productNames
            exchangeSetEndpoint.MapPost("/productNames", async (
                List<string> productNames,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                string? callbackUri = null) =>
            {
                return Results.Accepted(null, CreateResponse(productNames.Count, 4, 1)); // Temporary response for demonstration purposes
            })
            .Produces<CustomExchangeSetResponse>(202)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Provide all the latest releasable baseline data for a specified set of S100 Products.");

            // POST /v2/exchangeSet/s100/productVersions
            exchangeSetEndpoint.MapPost("/productVersions", async (
                List<S100ProductVersionsRequest> productVersions,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                string? callbackUri = null) =>
            {
                return Results.Accepted(null, CreateResponse(productVersions.Count, 5, 1)); // Temporary response for demonstration purposes
            })
            .Produces<CustomExchangeSetResponse>(202)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Given a set of S100 Product versions (e.g. Edition x Update y) provide any later releasable files.");

            // POST /v2/exchangeSet/s100/updatesSince
            exchangeSetEndpoint.MapPost("/updatesSince", async (
                S100UpdatesSinceRequest request,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                string? callbackUri = null,
                string? productIdentifier = null) =>
            {
                return Results.Accepted(null, CreateResponse(7, 6, 1));  // Temporary response for demonstration purposes
            })
            .Produces<CustomExchangeSetResponse>(202)
            .Produces(304)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Provide all the releasable S100 data after a datetime.");
        }

        // Temporary method to create a response for demonstration purposes.
        private static CustomExchangeSetResponse CreateResponse(
            int requestedProductCount,
            int exchangeSetProductCount,
            int requestedProductsAlreadyUpToDateCount)
        {
            var batchId = Guid.NewGuid().ToString("N"); // Simulate batch ID for demonstration purposes

            return new CustomExchangeSetResponse
            {
                Links = new ExchangeSetLinks
                {
                    ExchangeSetBatchStatusUri = new Link { Href = $"https://fss.ukho.gov.uk/batch/{batchId}/status" },
                    ExchangeSetBatchDetailsUri = new Link { Href = $"https://fss.ukho.gov.uk/batch/{batchId}" },
                    ExchangeSetFileUri = batchId != null ? new Link { Href = $"https://fss.ukho.gov.uk/batch/{batchId}/files/exchangeset.zip" } : null
                },
                ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(7), // TODO: Get from configuration
                RequestedProductCount = requestedProductCount,
                ExchangeSetProductCount = exchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = requestedProductsAlreadyUpToDateCount,
                RequestedProductsNotInExchangeSet =
                [
                    new ProductNotInExchangeSet
                        {
                            ProductName = "101GB40079ABCDEFG",
                            Reason = ProductNotIncludedReason.InvalidProduct
                        }
                ],
                FssBatchId = BatchId.From(batchId)
            };
        }
    }
}

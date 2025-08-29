using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineParameters
    {
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required ProductNameList Products { get; init; }

        public required string Filter { get; init; }

        public required JobId JobId { get; init; }

        public required IConfiguration Configuration { get; init; }

        /// <summary>
        /// The original request type for S100 endpoints
        /// </summary>
        public Messages.RequestType? RequestType { get; init; }

        /// <summary>
        /// The callback URI for asynchronous notifications
        /// </summary>
        public string? CallbackUri { get; init; }

        /// <summary>
        /// Product identifier filter for S100 updates since requests (s101, s102, s104, s111)
        /// </summary>
        public string? ProductIdentifier { get; init; }

        public Job CreateJob()
        {
            return new Job()
            {
                Id = JobId,
                Timestamp = Timestamp,
                DataStandard = DataStandard,
                RequestedProducts = Products,
                RequestedFilter = Filter,
                BatchId = BatchId.None,
                CallbackUri = CallbackUri
            };
        }

        public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration,
            string correlationId) =>
            new()
            {
                Version = MessageVersion.From(1), // Default version since JobRequestApiMessage doesn't have Version
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = message.Products,
                Filter = message.Filter,
                JobId = Domain.Jobs.JobId.From(correlationId),
                Configuration = configuration
            };

        /// <summary>
        /// Creates parameters from S100 Product Names request
        /// </summary>
        public static AssemblyPipelineParameters CreateFromS100ProductNames(List<string> productNames,
            IConfiguration configuration, string correlationId, string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameList(productNames),
                Filter = "productNames",
                JobId = Domain.Jobs.JobId.From(correlationId),
                Configuration = configuration,
                RequestType = Messages.RequestType.ProductNames,
                CallbackUri = callbackUri
            };

        /// <summary>
        /// Creates parameters from S100 Product Versions request
        /// </summary>
        public static AssemblyPipelineParameters CreateFromS100ProductVersions(S100ProductVersionsRequest request,
            IConfiguration configuration, string correlationId, string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameListFromVersions(request.ProductVersions),
                Filter = "productVersions",
                JobId = Domain.Jobs.JobId.From(correlationId),
                Configuration = configuration,
                RequestType = Messages.RequestType.ProductVersions,
                CallbackUri = callbackUri
            };

        /// <summary>
        /// Creates parameters from S100 Updates Since request
        /// </summary>
        public static AssemblyPipelineParameters CreateFromS100UpdatesSince(S100UpdatesSinceRequest request,
            IConfiguration configuration, string correlationId, string? productIdentifier = null,
            string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameListFromString("all"),
                Filter =
                    $"updatesSince:{request.SinceDateTime:O}" +
                    (productIdentifier != null ? $",productIdentifier:{productIdentifier}" : ""),
                JobId = Domain.Jobs.JobId.From(correlationId),
                Configuration = configuration,
                RequestType = Messages.RequestType.UpdatesSince,
                ProductIdentifier = productIdentifier,
                CallbackUri = callbackUri
            };

        /// <summary>
        /// Helper method to create ProductNameList from a list of product names
        /// </summary>
        private static ProductNameList CreateProductNameList(List<string> productNames)
        {
            var productNameList = new ProductNameList();
            foreach (var productName in productNames)
            {
                productNameList.Add(ProductName.From(productName));
            }
            return productNameList;
        }

        /// <summary>
        /// Helper method to create ProductNameList from a single string (used for "all" products)
        /// </summary>
        private static ProductNameList CreateProductNameListFromString(string productString)
        {
            var productNameList = new ProductNameList();
            if (!string.IsNullOrEmpty(productString) && productString != "all")
            {
                productNameList.Add(ProductName.From(productString));
            }
            return productNameList;
        }

        /// <summary>
        /// Helper method to create ProductNameList from S100ProductVersions
        /// </summary>
        private static ProductNameList CreateProductNameListFromVersions(List<S100ProductVersion> productVersions)
        {
            var productNameList = new ProductNameList();
            foreach (var productVersion in productVersions)
            {
                productNameList.Add(ProductName.From(productVersion.ProductName));
            }
            return productNameList;
        }
    }
}

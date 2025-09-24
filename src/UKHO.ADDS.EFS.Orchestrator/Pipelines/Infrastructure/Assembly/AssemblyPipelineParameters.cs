using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

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
        public RequestType RequestType { get; init; }

        /// <summary>
        /// The callback URI for asynchronous notifications
        /// </summary>
        public CallbackUri CallbackUri { get; init; }

        /// <summary>
        /// Product identifier filter for S100 updates since requests (s101, s102, s104, s111)
        /// </summary>
        public DataStandardProduct ProductIdentifier { get; init; }

        public IEnumerable<ProductVersion> ProductVersions { get; init; }

        public Job CreateJob() => new()
        {
            Id = JobId,
            Timestamp = Timestamp,
            DataStandard = DataStandard,
            RequestedProducts = Products,
            RequestedFilter = Filter,
            BatchId = BatchId.None,
            RequestType = RequestType,
            CallbackUri = CallbackUri,
            ProductIdentifier = ProductIdentifier,
            ProductVersions = ProductVersions,
        };

        public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration, CorrelationId correlationId)
        {
            return new AssemblyPipelineParameters()
            {
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = CreateProductNameList(message.Products),
                Filter = message.Filter,
                JobId = JobId.From((string)correlationId),
                Configuration = configuration,
                RequestType = RequestType.Internal,
                ProductIdentifier = DataStandardProduct.Undefined,
                CallbackUri = CallbackUri.None
            };
        }

        public static AssemblyPipelineParameters CreateFromS100ProductNames(List<string> productNames,
            IConfiguration configuration, string correlationId, string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameList(productNames.ToArray()),
                Filter = "productNames",
                JobId = JobId.From(correlationId),
                Configuration = configuration,
                RequestType = RequestType.ProductNames,
                ProductIdentifier = DataStandardProduct.Undefined,
                CallbackUri = !string.IsNullOrEmpty(callbackUri) ? CallbackUri.From(new Uri(callbackUri)) : CallbackUri.None
            };

        /// <summary>
        /// Creates parameters from S100 Product Versions request
        /// </summary>
        public static AssemblyPipelineParameters CreateFromS100ProductVersions(IEnumerable<ProductVersionRequest> productVersions,
            IConfiguration configuration, string correlationId, string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameListFromVersions(productVersions),
                Filter = "productVersions",
                JobId = JobId.From(correlationId),
                Configuration = configuration,
                RequestType = RequestType.ProductVersions,
                ProductIdentifier = DataStandardProduct.Undefined,
                CallbackUri = !string.IsNullOrEmpty(callbackUri) ? CallbackUri.From(new Uri(callbackUri)) : CallbackUri.None,
                ProductVersions = ConvertProductVersionRequests(productVersions)
            };

        /// <summary>
        /// Creates parameters from S100 Updates Since request
        /// </summary>
        public static AssemblyPipelineParameters CreateFromS100UpdatesSince(UpdatesSinceRequest request,
            IConfiguration configuration, string correlationId, string? productIdentifier = null,
            string? callbackUri = null) =>
            new()
            {
                Version = MessageVersion.From(2),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                Products = CreateProductNameListFromString(string.Empty),
                Filter =
                    $"updatesSince:{request.SinceDateTime:O}" +
                    (productIdentifier != null ? $",productIdentifier:{productIdentifier}" : ""),
                JobId = JobId.From(correlationId),
                Configuration = configuration,
                RequestType = RequestType.UpdatesSince,
                ProductIdentifier = !string.IsNullOrEmpty(productIdentifier) ? DataStandardProduct.FromEnum(Enum.Parse<DataStandardProductType>(productIdentifier.ToUpper())) : DataStandardProduct.Undefined,
                CallbackUri = !string.IsNullOrEmpty(callbackUri) ? CallbackUri.From(new Uri(callbackUri)) : CallbackUri.None
            };

        private static ProductNameList CreateProductNameList(string[] messageProducts)
        {
            var list = new ProductNameList();

            try
            {
                foreach (var product in messageProducts.Where(s => !string.IsNullOrEmpty(s))) // Scalar UI adds an empty product name by default as a placeholder/example
                {
                    list.Add(ProductName.From(product));
                }
            }
            catch (ValidationException ex)
            {
                throw new ArgumentException("One or more product names are invalid", ex);
            }

            return list;
        }

        /// <summary>
        /// Creates a ProductNameList from S100 product versions
        /// </summary>
        private static ProductNameList CreateProductNameListFromVersions(IEnumerable<ProductVersionRequest> productVersions)
        {
            var list = new ProductNameList();

            try
            {
                foreach (var productVersion in productVersions.Where(pv => !string.IsNullOrEmpty(pv.ProductName)))
                {
                    list.Add(ProductName.From(productVersion.ProductName!));
                }
            }
            catch (ValidationException ex)
            {
                throw new ArgumentException("One or more product names from versions are invalid", ex);
            }

            return list;
        }

        /// <summary>
        /// Creates a ProductNameList from a single string value
        /// </summary>
        private static ProductNameList CreateProductNameListFromString(string productName)
        {
            var list = new ProductNameList();

            try
            {
                if (!string.IsNullOrEmpty(productName))
                {
                    list.Add(ProductName.From(productName));
                }
            }
            catch (ValidationException ex)
            {
                throw new ArgumentException($"Product name '{productName}' is invalid", ex);
            }

            return list;
        }

        /// <summary>
        /// Converts ProductVersionRequest list to ProductVersion list
        /// </summary>
        private static List<ProductVersion> ConvertProductVersionRequests(IEnumerable<ProductVersionRequest> requests)
        {
            var result = new List<ProductVersion>();
            foreach (var req in requests)
            {
                if (!string.IsNullOrEmpty(req.ProductName) && req.EditionNumber.HasValue)
                {
                    result.Add(new ProductVersion
                    {
                        ProductName = ProductName.From(req.ProductName),
                        EditionNumber = EditionNumber.From(req.EditionNumber.Value),
                        UpdateNumber = req.UpdateNumber.HasValue ? UpdateNumber.From(req.UpdateNumber.Value) : UpdateNumber.From(0)
                    });
                }
            }
            return result;
        }
    }
}

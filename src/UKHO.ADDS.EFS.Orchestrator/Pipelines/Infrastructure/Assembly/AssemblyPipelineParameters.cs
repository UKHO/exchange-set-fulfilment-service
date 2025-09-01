using UKHO.ADDS.EFS.Domain.Exceptions;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;

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

        public Job CreateJob()
        {
            return new Job()
            {
                Id = JobId,
                Timestamp = Timestamp,
                DataStandard = DataStandard,
                RequestedProducts = Products,
                RequestedFilter = Filter,
                BatchId = BatchId.None
            };
        }

        public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration, CorrelationId correlationId)
        {
            return new AssemblyPipelineParameters()
            {
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = CreateProductNameList(message.Products),
                Filter = message.Filter,
                JobId = JobId.From((string)correlationId),
                Configuration = configuration
            };
        }

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
    }
}

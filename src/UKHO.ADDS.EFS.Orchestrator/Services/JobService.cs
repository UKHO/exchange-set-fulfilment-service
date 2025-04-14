using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class JobService
    {
        private readonly string _salesCatalogueServiceEndpoint;
        private readonly ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;

        // TODO Inject the SCS client here

        public JobService(string salesCatalogueServiceEndpoint, ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable)
        {
            _salesCatalogueServiceEndpoint = salesCatalogueServiceEndpoint;
            _jobTable = jobTable;
            _timestampTable = timestampTable;
        }

        public async Task<ExchangeSetJob> CreateJob(ExchangeSetRequestMessage request)
        {
            var job = await CreateJobEntity(request);

            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            await _timestampTable.CreateIfNotExistsAsync();
            var timestampResult = await _timestampTable.GetAsync(timestampKey, timestampKey);

            var timestamp = DateTime.MinValue;

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                // We have an existing timestamp from SCS
                timestamp = timestampEntity!.Timestamp;
            }

            var productInfo = await GetProductJson(request.Products, timestamp);

            job.State = productInfo.json.Equals(string.Empty, StringComparison.InvariantCultureIgnoreCase) ? ExchangeSetJobState.Cancelled : ExchangeSetJobState.Created;

            job.Products = productInfo.json;
            job.SalesCatalogueTimestamp = productInfo.scsTimestamp;

            await _jobTable.CreateIfNotExistsAsync();
            await _jobTable.AddAsync(job);

            return job;
        }

        public async Task CompleteJobAsync(long exitCode, ExchangeSetJob job)
        {
            // This should be the success path - set to 'Failed' to demonstrate writing the timestamp
            // All jobs currently 'fail', because the builder reports that a number of pipeline stages return 'NotRun'
            if (exitCode == BuilderExitCodes.Failed)
            {
                var updateTimestampEntity = new ExchangeSetTimestamp()
                {
                    DataStandard = job.DataStandard,
                    Timestamp = job.SalesCatalogueTimestamp
                };

                await _timestampTable.UpsertAsync(updateTimestampEntity);

                job.State = ExchangeSetJobState.Succeeded;
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
            }

            await _jobTable.UpdateAsync(job);
        }

        private Task<(string json, DateTime scsTimestamp)> GetProductJson(string requestProducts, DateTime timestamp)
        {
            // TODO Call SCS and get the product list - all for now. 'string json' will be POCO from SCS model
            const string productJson = "{ \"value\":123 }";

            // If SCS returns 304, the job is just marked as cancelled and processing stops
            // Simulate with "none" in request message for now. Timestamp would be SCS timestamp.
            if (requestProducts.Equals("none", StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult((string.Empty, DateTime.UtcNow));
            }

            // Would be SCS timestamp - we will update that if the job succeeds
            return Task.FromResult((productJson, DateTime.UtcNow));
        }

        private Task<ExchangeSetJob> CreateJobEntity(ExchangeSetRequestMessage request)
        {
            var id = Guid.NewGuid().ToString("N"); // TODO: details comment

            var job = new ExchangeSetJob()
            {
                Id = id,
                DataStandard = request.DataStandard,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
            };

            return Task.FromResult(job);
        }
    }
}

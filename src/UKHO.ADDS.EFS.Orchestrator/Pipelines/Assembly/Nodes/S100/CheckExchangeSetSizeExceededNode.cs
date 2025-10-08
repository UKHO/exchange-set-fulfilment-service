using System.Security.Claims;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CheckExchangeSetSizeExceededNode : AssemblyPipelineNode<S100Build>
    {
        private const string AudienceClaimType = "aud";
        private const string IssuerClaimType = "iss";
        private const string ExchangeSetSizeSource = "exchangeSetSize";
        private const string MaxExchangeSetSizeInMBConfigKey = "orchestrator:Response:MaxExchangeSetSizeInMB";
        private const string ExchangeSetSizeExceededErrorMessageConfigKey = "orchestrator:Errors:ExchangeSetSizeExceededMessage";

        private readonly ILogger<CheckExchangeSetSizeExceededNode> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckExchangeSetSizeExceededNode(AssemblyNodeEnvironment nodeEnvironment, ILogger<CheckExchangeSetSizeExceededNode> logger, IHttpContextAccessor httpContextAccessor) : base(nodeEnvironment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created && context.Subject.Job.ExchangeSetType != ExchangeSetType.Complete);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var addsEnvironment = AddsEnvironment.GetEnvironment();
            bool shouldCheckSize;

            if (addsEnvironment.IsDev() || addsEnvironment.IsLocal())
            {
                // In Dev and Local environments, always check the exchange set size limit as we are using mock endpoints
                shouldCheckSize = true;
            }
            else
            {
                // In other environments, only check the exchange set size limit for B2C users
                shouldCheckSize = IsB2CUser();
            }

            return shouldCheckSize && await IsExchangeSetSizeExceeded(context, context.Subject.Job)
                ? NodeResultStatus.Failed
                : NodeResultStatus.Succeeded;
        }

        /// <summary>
        /// Determines whether the current user is a B2C user by validating JWT token claims against Azure B2C configuration
        /// </summary>
        /// <returns>True if the user is authenticated via B2C; otherwise, false</returns>
        private bool IsB2CUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return false;
            }
            var tokenAudience = user.FindFirstValue(AudienceClaimType);
            var tokenIssuer = user.FindFirstValue(IssuerClaimType);

            var b2cClientId = System.Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppClientId);
            var b2cInstance = System.Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance);
            var b2cTenantId = System.Environment.GetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppTenantId);

            var b2CAuthority = $"{b2cInstance}{b2cTenantId}/v2.0/";
            var audience = b2cClientId;

            return string.Equals(tokenIssuer, b2CAuthority, StringComparison.OrdinalIgnoreCase) && string.Equals(tokenAudience, audience, StringComparison.Ordinal);
        }

        /// <summary>
        /// Validates whether the S100 Exchange Set size exceeds the configured maximum limit and handles error response generation
        /// </summary>
        /// <param name="context">The execution context containing the S100 build and product editions</param>
        /// <param name="job">The job instance for correlation ID in error responses</param>
        /// <returns>True if the exchange set size exceeds the limit; otherwise, false</returns>
        private async Task<bool> IsExchangeSetSizeExceeded(IExecutionContext<PipelineContext<S100Build>> context, Job job)
        {
            // Calculate total file size and check against the limit
            var maxExchangeSetSizeInMB = Environment.Configuration.GetValue<int>(MaxExchangeSetSizeInMBConfigKey);
            var exchangeSetSizeExceededErrorMessage = Environment.Configuration.GetValue<string>(ExchangeSetSizeExceededErrorMessageConfigKey);
            var totalFileSizeBytes = context.Subject.Build.ProductEditions.Sum(p => (long)p.FileSize);
            var bytesToKbFactor = 1024f;
            var totalFileSizeInMB = (totalFileSizeBytes / bytesToKbFactor) / bytesToKbFactor;

            if (totalFileSizeInMB > maxExchangeSetSizeInMB)
            {
                _logger.LogExchangeSetSizeExceeded((long)totalFileSizeInMB, maxExchangeSetSizeInMB);

                // Set up the error response for payload too large
                context.Subject.ErrorResponse = new ErrorResponseModel
                {
                    CorrelationId = job.Id.ToString(),
                    Errors =
                    [
                        new ErrorDetail
                        {
                             Source = ExchangeSetSizeSource,
                             Description = exchangeSetSizeExceededErrorMessage!
                        }
                    ]
                };

                await context.Subject.SignalAssemblyError();

                return true;
            }

            return false;
        }
    }
}

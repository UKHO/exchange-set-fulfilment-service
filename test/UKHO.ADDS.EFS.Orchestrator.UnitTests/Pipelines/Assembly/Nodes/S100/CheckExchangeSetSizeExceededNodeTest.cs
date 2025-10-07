using System.Security.Claims;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Assembly.Nodes.S100
{
    [TestFixture]
    internal class CheckExchangeSetSizeExceededNodeTests
    {
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private ILogger<CheckExchangeSetSizeExceededNode> _logger;
        private IHttpContextAccessor _httpContextAccessor;
        private IStorageService _storageService;
        private IConfiguration _configuration;
        private CheckExchangeSetSizeExceededNode _checkExchangeSetSizeExceededNode;
        private CancellationToken _cancellationToken;
        private HttpContext _httpContext;
        private ClaimsPrincipal _user;

        private const string DefaultMaxExchangeSetSizeMB = "300";
        private const string TestJobId = "test-job-id";
        private const string TestCallbackUri = "https://test.com/callback";
        private const string TestB2CClientId = "test-b2c-client-id";
        private const string TestB2CInstance = "https://test.b2clogin.com/";
        private const string TestB2CTenantId = "test-tenant-id";
        private const string TestB2CAuthority = "https://test.b2clogin.com/test-tenant-id/v2.0/";
        private const string ExchangeSetSizeExceededErrorMessage = "The Exchange Set requested is large and will not be created, please use a standard Exchange Set provided by the UKHO.";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            _logger = A.Fake<ILogger<CheckExchangeSetSizeExceededNode>>();
            _httpContextAccessor = A.Fake<IHttpContextAccessor>();
            _httpContext = A.Fake<HttpContext>();
            _storageService = A.Fake<IStorageService>();
            _cancellationToken = CancellationToken.None;

            _configuration = CreateTestConfiguration();
            _nodeEnvironment = new AssemblyNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>());
        }

        [SetUp]
        public void Setup()
        {
            _checkExchangeSetSizeExceededNode = new CheckExchangeSetSizeExceededNode(_nodeEnvironment, _logger, _httpContextAccessor);
            ResetEnvironmentVariables();
            _user = new ClaimsPrincipal();
            A.CallTo(() => _httpContextAccessor.HttpContext).Returns(_httpContext);
            A.CallTo(() => _httpContext.User).Returns(_user);
        }

        [TearDown]
        public void TearDown()
        {
            ResetEnvironmentVariables();
        }

        private static void ResetEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppClientId, null);
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance, null);
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppTenantId, null);
            Environment.SetEnvironmentVariable(WellKnownConfigurationName.AddsEnvironmentName, null);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLoggerException = Assert.Throws<ArgumentNullException>(() =>
                new CheckExchangeSetSizeExceededNode(_nodeEnvironment, null!, _httpContextAccessor));
            var nullHttpContextAccessorException = Assert.Throws<ArgumentNullException>(() =>
                new CheckExchangeSetSizeExceededNode(_nodeEnvironment, _logger, null!));

            Assert.That(nullLoggerException.ParamName, Is.EqualTo("logger"));
            Assert.That(nullHttpContextAccessorException.ParamName, Is.EqualTo("httpContextAccessor"));
        }

        [TestCase(JobState.Created, RequestType.ProductNames, ExpectedResult = true)]
        [TestCase(JobState.Created, RequestType.ProductVersions, ExpectedResult = true)]
        [TestCase(JobState.Created, RequestType.UpdatesSince, ExpectedResult = true)]
        [TestCase(JobState.Created, RequestType.Internal, ExpectedResult = false)]
        [TestCase(JobState.UpToDate, RequestType.ProductNames, ExpectedResult = false)]
        public async Task<bool> WhenJobStateAndRequestTypeProvided_ThenShouldExecuteAsyncReturnsCorrectResult(
            JobState jobState, RequestType requestType)
        {
            var job = CreateTestJob(requestType: requestType);
            job.ValidateAndSet(jobState, BuildState.None);
            var pipelineContext = CreatePipelineContext(job);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            return await _checkExchangeSetSizeExceededNode.ShouldExecuteAsync(_executionContext);
        }

        [TestCase("dev", 350 * 1024 * 1024, true, true, NodeResultStatus.Failed)] // Dev environment, size exceeded, B2C user
        [TestCase("dev", 50 * 1024 * 1024, true, true, NodeResultStatus.Succeeded)] // Dev environment, size within limit, B2C user
        [TestCase("preprod", 1000, false, false, NodeResultStatus.Succeeded)] // Non-dev environment, default size, non-B2C user
        [TestCase("preprod", 350 * 1024 * 1024, false, false, NodeResultStatus.Succeeded)] // Non-dev environment, size exceeded, non-B2C user
        [TestCase("preprod", 350 * 1024 * 1024, true, true, NodeResultStatus.Failed)] // Non-dev environment, size exceeded, B2C user
        [TestCase("preprod", 50 * 1024 * 1024, true, true, NodeResultStatus.Succeeded)] // Non-dev environment, size within limit, B2C user
        [TestCase("preprod", 300 * 1024 * 1024, true, true, NodeResultStatus.Succeeded)] // Non-dev environment, size at limit, B2C user
        public async Task WhenEnvironmentAndUserTypeAndSizeProvided_ThenExecuteAsyncReturnsExpectedResult(
            string environment, int fileSizeBytes, bool isB2CUser, bool setB2CEnvironmentVars, NodeResultStatus expectedResult)
        {
            var job = CreateTestJob();
            var build = CreateS100BuildWithProductEditions(fileSizeBytes);
            SetupExecutionContextWithBuild(job, build);

            if (isB2CUser)
            {
                SetupValidB2CUser();
            }
            else
            {
                SetupNonB2CUser();
            }

            if (setB2CEnvironmentVars)
            {
                SetupB2CEnvironmentVariables();
            }
            SetAddsEnvironment(environment);

            var result = await _checkExchangeSetSizeExceededNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(expectedResult));
        }

        [TestCase("wrong-audience", TestB2CAuthority, NodeResultStatus.Succeeded)] // Wrong audience
        [TestCase(TestB2CClientId, "https://wrong-issuer/", NodeResultStatus.Succeeded)] // Wrong issuer
        [TestCase("", "", NodeResultStatus.Succeeded)] // Missing claims
        public async Task WhenB2CUserWithInvalidClaimsAndSizeExceedsLimit_ThenExecuteAsyncReturnsSucceeded(
            string audience, string issuer, NodeResultStatus expectedResult)
        {
            var job = CreateTestJob();
            var build = CreateS100BuildWithProductEditions(350 * 1024 * 1024); // 350MB (exceeds 300MB limit)
            SetupExecutionContextWithBuild(job, build);
            SetupB2CEnvironmentVariables();
            SetAddsEnvironment("preprod");

            if (string.IsNullOrEmpty(audience) && string.IsNullOrEmpty(issuer))
            {
                // Set up user with missing claims (empty ClaimsPrincipal)
                _user = new ClaimsPrincipal();
                A.CallTo(() => _httpContext.User).Returns(_user);
            }
            else
            {
                SetupUserWithClaims(audience, issuer);
            }

            var result = await _checkExchangeSetSizeExceededNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task WhenB2CUserAndExchangeSetSizeExceedsLimit_ThenSetsLogsAndErrorResponse()
        {
            var job = CreateTestJob();
            var build = CreateS100BuildWithProductEditions(350 * 1024 * 1024); // 350MB (exceeds 300MB limit)
            SetupExecutionContextWithBuild(job, build);
            SetupValidB2CUser();
            SetupB2CEnvironmentVariables();
            SetAddsEnvironment("preprod");

            await _checkExchangeSetSizeExceededNode.ExecuteAsync(_executionContext);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                  LogLevel.Warning,
                  A<EventId>.That.Matches(e => e.Name == "LogExchangeSetSizeExceeded"),
                  A<LoggerMessageState>._,
                  null,
                  A<Func<LoggerMessageState, Exception?, string>>._));

            Assert.That(_executionContext.Subject.ErrorResponse, Is.Not.Null);
            Assert.That(_executionContext.Subject.ErrorResponse.CorrelationId, Is.EqualTo(job.Id.ToString()));
            Assert.That(_executionContext.Subject.ErrorResponse.Errors, Has.Count.EqualTo(1));
            Assert.That(_executionContext.Subject.ErrorResponse.Errors.First().Source, Is.EqualTo("exchangeSetSize"));
            Assert.That(_executionContext.Subject.ErrorResponse.Errors.First().Description, Is.EqualTo(ExchangeSetSizeExceededErrorMessage));
        }

        private static IConfiguration CreateTestConfiguration(string maxSizeMB = DefaultMaxExchangeSetSizeMB)
        {
            var configurationData = new Dictionary<string, string>
            {
                { "orchestrator:Response:MaxExchangeSetSizeInMB", maxSizeMB },
                { "orchestrator:Errors:ExchangeSetSizeExceededMessage", ExchangeSetSizeExceededErrorMessage }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
        }

        private static Job CreateTestJob(RequestType requestType = RequestType.ProductNames)
        {
            return new Job
            {
                Id = JobId.From(TestJobId),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = string.Empty,
                RequestType = requestType,
                CallbackUri = CallbackUri.From(new Uri(TestCallbackUri)),
                ProductIdentifier = DataStandardProduct.Undefined
            };
        }

        private static S100Build CreateS100BuildWithProductEditions(int fileSizeBytes = 1000)
        {
            return new S100Build
            {
                ProductEditions = new List<ProductEdition>
                {
                    new() { ProductName = ProductName.From("101GB40079ABCDEFG"), FileSize = fileSizeBytes }
                }
            };
        }

        private void SetupExecutionContextWithBuild(Job job, S100Build build)
        {
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);
        }

        private PipelineContext<S100Build> CreatePipelineContext(Job job)
        {
            var build = new S100Build();
            return new PipelineContext<S100Build>(job, build, _storageService);
        }

        private void SetupValidB2CUser()
        {
            SetupUserWithClaims(TestB2CClientId, TestB2CAuthority);
        }

        private void SetupNonB2CUser()
        {
            SetupUserWithClaims("different-audience", "https://different-issuer/");
        }

        private void SetupUserWithClaims(string audience, string issuer)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(audience))
            {
                claims.Add(new Claim("aud", audience));
            }
            if (!string.IsNullOrEmpty(issuer))
            {
                claims.Add(new Claim("iss", issuer));
            }

            var identity = new ClaimsIdentity(claims);
            _user = new ClaimsPrincipal(identity);
            A.CallTo(() => _httpContext.User).Returns(_user);
        }

        private void SetupB2CEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppClientId, TestB2CClientId);
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppInstance, TestB2CInstance);
            Environment.SetEnvironmentVariable(GlobalEnvironmentVariables.EfsB2CAppTenantId, TestB2CTenantId);
        }

        private void SetAddsEnvironment(string env)
        {
            // Set environment variable for AddsEnvironment
            if (env == "dev")
            {
                Environment.SetEnvironmentVariable(WellKnownConfigurationName.AddsEnvironmentName, "dev");
            }
            else
            {
                Environment.SetEnvironmentVariable(WellKnownConfigurationName.AddsEnvironmentName, "preprod");
            }
        }
    }
}

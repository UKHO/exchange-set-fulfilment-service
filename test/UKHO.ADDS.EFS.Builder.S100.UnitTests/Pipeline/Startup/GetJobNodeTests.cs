using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Startup
{
    internal class GetJobNodeTests
    {
        private GetJobNode _node;
        private IExecutionContext<ExchangeSetPipelineContext> _context;
        private ExchangeSetPipelineContext _subject;
        private IConfiguration _configuration;
        private INodeStatusWriter _nodeStatusWriter;
        private IToolClient _toolClient;
        private ILoggerFactory _loggerFactory;

        [SetUp]
        public void SetUp()
        {
            _configuration = A.Fake<IConfiguration>();
            _nodeStatusWriter = A.Fake<INodeStatusWriter>();
            _toolClient = A.Fake<IToolClient>();
            _loggerFactory = A.Fake<ILoggerFactory>();

            _subject = new ExchangeSetPipelineContext(_configuration, _nodeStatusWriter, _toolClient, _loggerFactory)
            {
                JobId = Guid.NewGuid().ToString(),
                BuildServiceEndpoint = "http://localhost",
                IsDebugSession = true
            };

            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_subject);

            _node = new GetJobNode();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncWithValidDebugJob_ThenSetsJobAndReturnsSucceeded()
        {
            var debugJob = new ExchangeSetJob
            {
                Id = "debug-id",
                DataStandard = ExchangeSetDataStandard.S100,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.InProgress,
                Products =
                [
                    new() {
                        ProductName = "TestProduct",
                        LatestEditionNumber = 1,
                        LatestUpdateNumber = 2,
                        Status = new S100ProductStatus
                        {
                            StatusName = "newDataset",
                            StatusDate = DateTime.UtcNow
                        }
                    }
                ]
            };

            _configuration = GetConfiguration(debugJob);

            _subject = new ExchangeSetPipelineContext(_configuration, _nodeStatusWriter, _toolClient, _loggerFactory)
            {
                JobId = Guid.NewGuid().ToString(),
                BuildServiceEndpoint = "http://localhost",
                IsDebugSession = true
            };
            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_subject);

            _node = new GetJobNode();

            var result = await _node.ExecuteAsync(_context);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_subject.Job, Is.Not.Null);
                Assert.That(_subject.JobId, Is.EqualTo(_subject.Job.Id));
                Assert.That(_subject.Job.DataStandard, Is.EqualTo(ExchangeSetDataStandard.S100));
                Assert.That(_subject.Job.State, Is.EqualTo(ExchangeSetJobState.InProgress));
            });
            Assert.That(_subject.Job.Products, Is.Not.Null);
            A.CallTo(() => _nodeStatusWriter.WriteDebugExchangeSetJob(A<ExchangeSetJob>._, A<string>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncWithNullDebugJob_ThenUsesDefaultValuesAndReturnsSucceeded()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()!)
                .Build();
            _subject = new ExchangeSetPipelineContext(_configuration, _nodeStatusWriter, _toolClient, _loggerFactory)
            {
                JobId = Guid.NewGuid().ToString(),
                BuildServiceEndpoint = "http://localhost",
                IsDebugSession = true
            };
            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_subject);

            _node = new GetJobNode();

            var result = await _node.ExecuteAsync(_context);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_subject.Job, Is.Not.Null);
                Assert.That(_subject.JobId, Is.EqualTo(_subject.Job.Id));
            });
            Assert.Multiple(() =>
            {
                Assert.That(_subject.Job.DataStandard, Is.EqualTo(ExchangeSetDataStandard.S100));
                Assert.That(_subject.Job.State, Is.EqualTo(ExchangeSetJobState.InProgress));
                Assert.That(_subject.Job.Products, Is.Not.Null);
            });
            Assert.That(_subject.Job.Products.Count, Is.EqualTo(9));
            foreach (var product in _subject.Job.Products)
            {
                Assert.That(product.ProductName, Is.Not.Empty);
                Assert.That(product.Status!.StatusName, Is.Not.Empty);
                Assert.That(product.Status.StatusDate, Is.LessThan(DateTime.UtcNow));
            }
            A.CallTo(() => _nodeStatusWriter.WriteDebugExchangeSetJob(A<ExchangeSetJob>._, A<string>._)).MustHaveHappened();
        }

        [TearDown]
        public void TearDown()
        {
            _loggerFactory?.Dispose();
        }

        private static IConfiguration GetConfiguration(ExchangeSetJob debugJob)
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"DebugJob:Id", debugJob.Id},
                {"DebugJob:DataStandard", ((int)debugJob.DataStandard).ToString()},
                {"DebugJob:Timestamp", debugJob.Timestamp.ToString("o")},
                {"DebugJob:State", ((int)debugJob.State).ToString()},
                {"DebugJob:Products:0:ProductName", debugJob.Products[0].ProductName!},
                {"DebugJob:Products:0:LatestEditionNumber", debugJob.Products[0].LatestEditionNumber.ToString()!},
                {"DebugJob:Products:0:LatestUpdateNumber", debugJob.Products[0].LatestUpdateNumber.ToString()!},
                {"DebugJob:Products:0:Status:StatusName", debugJob.Products[0].Status!.StatusName!},
                {"DebugJob:Products:0:Status:StatusDate", debugJob.Products[0].Status!.StatusDate.ToString("o")}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }
    }
}

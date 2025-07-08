using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FakeItEasy;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.UnitTests.Extensions;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Table
{
    public class ExchangeSetTimestampTableTest
    {
        private ExchangeSetTimestampTable _exchangeSetTimestampTable;
        private TableServiceClient _fakeTableServiceClient;
        private TableClient _fakeTableClient;
        private const string PartitionKey = "validPartitionKey";
        private const string RowKey = "validRowKey";
        private ExchangeSetTimestamp _entity;

        [SetUp]
        public void SetUp()
        {
            _fakeTableServiceClient = A.Fake<TableServiceClient>();
            _fakeTableClient = A.Fake<TableClient>();

            A.CallTo(() => _fakeTableServiceClient.GetTableClient(A<string>.Ignored))
                .Returns(_fakeTableClient);

            _exchangeSetTimestampTable = new ExchangeSetTimestampTable(_fakeTableServiceClient);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
             _entity = new ExchangeSetTimestamp
            {
                DataStandard = ExchangeSetDataStandard.S100,
                Timestamp = DateTime.UtcNow
            };
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncCalled_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeTableClient.CreateIfNotExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<Response<TableItem>>(null!));

            var result = await _exchangeSetTimestampTable.CreateIfNotExistsAsync(CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeTableClient.CreateIfNotExistsAsync(A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetTimestampTable.CreateIfNotExistsAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error creating table: Test exception"));
            });
        }

        [Test]
        public async Task WhenGetAsyncCalledWithEmptyEntity_ThenReturnsFalse()
        {
            A.CallTo(() => _fakeTableClient.QueryAsync(
                    A<Expression<Func<JsonEntity, bool>>>.Ignored,
                    A<int?>.Ignored,
                    A<List<string>>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(new List<JsonEntity>().CreateAsyncPageable());

            var result = await _exchangeSetTimestampTable.GetUniqueAsync(PartitionKey, RowKey);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task WhenGetAsyncCalledWithValidEntity_ThenReturnsTrue()
        {
            var fakeTable = new List<JsonEntity>
            {
                new()
                {
                    PartitionKey = "s100",
                    RowKey = "s100",
                    P0 = "{\"DataStandard\":\"s100\",\"Timestamp\":\"2023-10-01T00:00:00Z\"}"
                }
            };

            A.CallTo(() => _fakeTableClient.QueryAsync(
                    A<Expression<Func<JsonEntity, bool>>>.Ignored,
                    A<int?>.Ignored,
                    A<List<string>>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(fakeTable.CreateAsyncPageable());

            var result = await _exchangeSetTimestampTable.GetUniqueAsync(PartitionKey, RowKey);
            result.IsSuccess(out var value, out var error);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(value?.DataStandard, Is.EqualTo(ExchangeSetDataStandard.S100));
                Assert.That(value?.Timestamp, Is.EqualTo(DateTime.Parse("2023-10-01T00:00:00Z").ToUniversalTime()));
            });
        }

        [Test]
        public async Task WhenGetAsyncCalledWithPartitionKeyAndNoEntitiesExist_ThenReturnsEmptyList()
        {
            A.CallTo(() => _fakeTableClient.QueryAsync(
                    A<Expression<Func<JsonEntity, bool>>>.Ignored,
                    A<int?>.Ignored,
                    A<List<string>>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(new List<JsonEntity>().CreateAsyncPageable());

            var result = await _exchangeSetTimestampTable.GetListAsync(PartitionKey);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task WhenGetAsyncCalledWithPartitionKeyAndEntitiesExist_ThenReturnsEntityList()
        {
            var fakeTable = new List<JsonEntity>
            {
                new()
                {
                    PartitionKey = PartitionKey,
                    RowKey = "row1",
                    P0 = "{\"DataStandard\":\"s100\",\"Timestamp\":\"2023-10-01T00:00:00Z\"}"
                },
                new()
                {
                    PartitionKey = PartitionKey,
                    RowKey = "row2",
                    P0 = "{\"DataStandard\":\"s63\",\"Timestamp\":\"2023-10-02T00:00:00Z\"}"
                }
            };

            A.CallTo(() => _fakeTableClient.QueryAsync(
                    A<Expression<Func<JsonEntity, bool>>>.Ignored,
                    A<int?>.Ignored,
                    A<List<string>>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(fakeTable.CreateAsyncPageable());

            var result = await _exchangeSetTimestampTable.GetListAsync(PartitionKey);

            Assert.Multiple(() =>
            {
                var exchangeSetTimestamps = result as ExchangeSetTimestamp[] ?? result.ToArray();
                Assert.That(exchangeSetTimestamps, Is.Not.Empty);
                Assert.That(exchangeSetTimestamps.Count(), Is.EqualTo(2));
                Assert.That(exchangeSetTimestamps.First().DataStandard, Is.EqualTo(ExchangeSetDataStandard.S100));
                Assert.That(exchangeSetTimestamps.First().Timestamp, Is.EqualTo(DateTime.Parse("2023-10-01T00:00:00Z").ToUniversalTime()));
                Assert.That(exchangeSetTimestamps.Last().DataStandard, Is.EqualTo(ExchangeSetDataStandard.S63));
                Assert.That(exchangeSetTimestamps.Last().Timestamp, Is.EqualTo(DateTime.Parse("2023-10-02T00:00:00Z").ToUniversalTime()));
            });
        }

        [Test]
        public async Task WhenAddAsyncCalledWithValidEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeTableClient.AddEntityAsync(A<JsonEntity>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<Response>(null!));

            var result = await _exchangeSetTimestampTable.AddAsync(_entity);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenAddAsyncThrowsInvalidOperationException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeTableClient.AddEntityAsync(A<JsonEntity>.Ignored, A<CancellationToken>.Ignored))
                .Throws(new InvalidOperationException("Test exception"));

            var result = await _exchangeSetTimestampTable.AddAsync(_entity);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
            });
        }

        [Test]
        public async Task WhenUpsertAsyncCalledWithValidEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeTableClient.UpsertEntityAsync(A<JsonEntity>.Ignored, TableUpdateMode.Merge, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<Response?>(null));

            var result = await _exchangeSetTimestampTable.UpsertAsync(_entity);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenUpsertAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeTableClient.UpsertEntityAsync(A<JsonEntity>.Ignored, TableUpdateMode.Merge, A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetTimestampTable.UpsertAsync(_entity);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
            });
        }
    }
}

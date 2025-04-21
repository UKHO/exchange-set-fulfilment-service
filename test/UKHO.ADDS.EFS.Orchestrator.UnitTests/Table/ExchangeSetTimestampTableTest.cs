using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FakeItEasy;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Table
{
    public class ExchangeSetTimestampTableTest
    {
        private ExchangeSetTimestampTable _exchangeSetTimestampTable;
        private TableServiceClient _fakeTableServiceClient;
        private TableClient _fakeTableClient;
        private const string PartitionKey = "validPartitionKey";
        private const string RowKey = "validRowKey";

        [SetUp]
        public void SetUp()
        {
            _fakeTableServiceClient = A.Fake<TableServiceClient>();
            _fakeTableClient = A.Fake<TableClient>();

            A.CallTo(() => _fakeTableServiceClient.GetTableClient(A<string>.Ignored))
                .Returns(_fakeTableClient);

            _exchangeSetTimestampTable = new ExchangeSetTimestampTable(_fakeTableServiceClient);
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncCalled_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeTableClient.CreateIfNotExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<Response<TableItem>>(null));

            var result = await _exchangeSetTimestampTable.CreateIfNotExistsAsync();

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeTableClient.CreateIfNotExistsAsync(A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetTimestampTable.CreateIfNotExistsAsync();

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
                .Returns(TestHelper.CreateAsyncPageable(new List<JsonEntity>()));

            var result = await _exchangeSetTimestampTable.GetAsync(PartitionKey, RowKey);

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
                .Returns(TestHelper.CreateAsyncPageable<JsonEntity>(fakeTable));

            var result = await _exchangeSetTimestampTable.GetAsync(PartitionKey, RowKey);
            result.IsSuccess(out var value, out var error);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(value?.DataStandard, Is.EqualTo(ExchangeSetDataStandard.S100));
                Assert.That(value?.Timestamp, Is.EqualTo(DateTime.Parse("2023-10-01T00:00:00Z").ToUniversalTime()));
            });
        }
    }
}

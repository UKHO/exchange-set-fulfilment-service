using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FakeItEasy;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Table
{
    public class ExchangeSetTimestampTableTest
    {
        private ExchangeSetTimestampTable _exchangeSetTimestampTable;
        private TableServiceClient _fakeTableServiceClient;
        private TableClient _fakeTableClient;
        private ExchangeSetTimestamp _testEntity;

        [SetUp]
        public void SetUp()
        {
            _fakeTableServiceClient = A.Fake<TableServiceClient>();
            _fakeTableClient = A.Fake<TableClient>();

            A.CallTo(() => _fakeTableServiceClient.GetTableClient(A<string>.Ignored))
                .Returns(_fakeTableClient);

            _exchangeSetTimestampTable = new ExchangeSetTimestampTable(_fakeTableServiceClient);

            _testEntity = new ExchangeSetTimestamp
            {
                DataStandard = ExchangeSetDataStandard.S63,
                Timestamp = DateTime.UtcNow
            };
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

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error creating table: Test exception"));
        }
    }
}

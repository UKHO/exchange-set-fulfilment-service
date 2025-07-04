using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Table
{
    public class ExchangeSetJobTableTest
    {
        private S100ExchangeSetJobTable _exchangeSetJobTable;
        private BlobServiceClient _fakeBlobServiceClient;
        private BlobContainerClient _fakeBlobContainerClient;
        private BlobClient _fakeBlobClient;
        private S100ExchangeSetJob _testEntity;
        private ILogger<BlobTable<S100ExchangeSetJob>> _fakeLogger;
        private const string PartitionKey = "validPartitionKey";
        private const string RowKey = "validRowKey";

        [SetUp]
        public void SetUp()
        {
            _fakeBlobServiceClient = A.Fake<BlobServiceClient>();
            _fakeBlobContainerClient = A.Fake<BlobContainerClient>();
            _fakeBlobClient = A.Fake<BlobClient>();

            A.CallTo(() => _fakeBlobServiceClient.GetBlobContainerClient(A<string>.Ignored))
                .Returns(_fakeBlobContainerClient);
            A.CallTo(() => _fakeBlobContainerClient.GetBlobClient(A<string>.Ignored))
                .Returns(_fakeBlobClient);

            _exchangeSetJobTable = new S100ExchangeSetJobTable(_fakeBlobServiceClient);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testEntity = new S100ExchangeSetJob
            {
                Id = "test-id",
                Products =
                [
                    new S100Products { ProductName = "Product1"},
                    new S100Products { ProductName = "Product2"}
                ],
                Timestamp = DateTime.UtcNow,
                SalesCatalogueTimestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
                DataStandard = ExchangeSetDataStandard.S63
            };
        }

        [Test]
        public async Task WhenAddAsyncCalledWithValidEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored, A<bool>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(A.Fake<Response<BlobContentInfo>>()));

            var result = await _exchangeSetJobTable.AddAsync(_testEntity);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenAddAsyncCalledAndUploadAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.UploadAsync(A<Stream>.Ignored, true, A<CancellationToken>.Ignored))
            .Throws(new Exception("Test exception"));

            var result = await _exchangeSetJobTable.AddAsync(_testEntity);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
        }

        [Test]
        public async Task WhenUpdateAsyncCalledWithExistingEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(true, A.Fake<Response>())));

            A.CallTo(() => _fakeBlobClient.UploadAsync(A<Stream>.Ignored, true, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(A.Fake<Response<BlobContentInfo>>()));

            var result = await _exchangeSetJobTable.UpdateAsync(_testEntity);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenUpdateAsyncCalledWithNonExistingEntity_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(false, A.Fake<Response>())));

            var result = await _exchangeSetJobTable.UpdateAsync(_testEntity);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task WhenUpdateAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetJobTable.UpdateAsync(_testEntity);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
            });
        }

        [Test]
        public async Task WhenUpsertAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored, A<bool>.Ignored, A<CancellationToken>.Ignored))
            .Throws(new Exception("Test exception"));

            var result = await _exchangeSetJobTable.UpsertAsync(_testEntity);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
            });
        }

        [Test]
        public async Task WhenUpsertAsyncCalled_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored, A<bool>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(A.Fake<Response<BlobContentInfo>>()));

            var result = await _exchangeSetJobTable.UpsertAsync(_testEntity);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenGetAsyncCalledWithExistingEntity_ThenReturnsEntity()
        {
            var serializedEntity = JsonCodec.Encode(_testEntity);
            var testEntityStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEntity));
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(true, A.Fake<Response>())));

            A.CallTo(() => _fakeBlobClient.DownloadAsync())
                .Returns(Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadInfo(content: testEntityStream), A.Fake<Response>())));

            var result = await _exchangeSetJobTable.GetAsync(PartitionKey, RowKey);
            result.IsSuccess(out var value, out var error);

            Assert.Multiple(() =>
            {
                Assert.That(value?.Products, Is.Not.Null);
                Assert.That(value?.Products.Count, Is.EqualTo(2));
                Assert.That(value?.Products[0].ProductName, Is.EqualTo("Product1"));
                Assert.That(value?.Products[1].ProductName, Is.EqualTo("Product2"));
            });
        }

        [Test]
        public async Task WhenGetAsyncCalledAndExistsAsyncThroeException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetJobTable.GetAsync(PartitionKey, PartitionKey);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
            });
        }

        [Test]
        public async Task WhenGetAsyncCalledWithNonExistingEntity_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(false, A.Fake<Response>())));

            var result = await _exchangeSetJobTable.GetAsync(PartitionKey, PartitionKey);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task WhenDeleteAsyncCalledWithExistingEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(true, A.Fake<Response>())));

            A.CallTo(() => _fakeBlobClient.DeleteIfExistsAsync(A<DeleteSnapshotsOption>.Ignored, A<BlobRequestConditions>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(true, A.Fake<Response>())));

            var result = await _exchangeSetJobTable.DeleteAsync(PartitionKey, PartitionKey);

            Assert.That(result.IsSuccess, Is.True);
            A.CallTo(() => _fakeBlobClient.DeleteIfExistsAsync(A<DeleteSnapshotsOption>.Ignored, A<BlobRequestConditions>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenDeleteAsyncCalledWithNonExistingEntity_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(false, A.Fake<Response>())));

            var result = await _exchangeSetJobTable.DeleteAsync(PartitionKey, PartitionKey);

            Assert.That(result.IsSuccess, Is.True);
            A.CallTo(() => _fakeBlobClient.DeleteIfExistsAsync(A<DeleteSnapshotsOption>.Ignored, A<BlobRequestConditions>.Ignored, A<CancellationToken>.Ignored))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task WhenDeleteAsyncCalledWithExistsAsyncThowException_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobClient.ExistsAsync(A<CancellationToken>.Ignored))
                .Throws(new Exception("Test exception"));

            var result = await _exchangeSetJobTable.DeleteAsync(PartitionKey, PartitionKey);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Error: Test exception"));
                A.CallTo(() => _fakeBlobClient.DeleteIfExistsAsync(A<DeleteSnapshotsOption>.Ignored, A<BlobRequestConditions>.Ignored, A<CancellationToken>.Ignored))
                    .MustNotHaveHappened();
            });
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncCalled_ThenReturnsSuccess()
        {
            A.CallTo(() => _fakeBlobContainerClient.CreateIfNotExistsAsync(
                    A<PublicAccessType>.Ignored,
                    A<IDictionary<string, string>>.Ignored,
                    A<BlobContainerEncryptionScopeOptions>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(
                        lastModified: DateTimeOffset.UtcNow,
                        eTag: ETag.All),
            A.Fake<Response>())));

            var result = await _exchangeSetJobTable.CreateIfNotExistsAsync(CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task WhenCreateIfNotExistsAsyncThrowsException_ThenReturnsFailure()
        {
            A.CallTo(() => _fakeBlobContainerClient.CreateIfNotExistsAsync(
                    A<PublicAccessType>.Ignored,
                    A<IDictionary<string, string>>.Ignored,
                    A<BlobContainerEncryptionScopeOptions>.Ignored,
                    A<CancellationToken>.Ignored))
                .Throws(new Exception("Error"));

            var result = await _exchangeSetJobTable.CreateIfNotExistsAsync(CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
        }
    }
}

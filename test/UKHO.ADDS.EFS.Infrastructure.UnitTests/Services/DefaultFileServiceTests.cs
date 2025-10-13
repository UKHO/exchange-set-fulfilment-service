using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.User;
using UKHO.ADDS.EFS.Infrastructure.Services;
using UKHO.ADDS.Infrastructure.Results;
using BatchIdentifier = UKHO.ADDS.EFS.Domain.Jobs.BatchId;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Services
{
    [TestFixture]
    public class DefaultFileServiceTests
    {
        private IFileShareReadWriteClient _fileShareReadWriteClient;
        private IConfiguration _configuration;
        private ILogger<DefaultFileService> _logger;
        private DefaultFileService _defaultFileService;
        private CorrelationId _correlationId;
        private CancellationToken _cancellationToken;
        private UserIdentifier _userIdentifier;
        private BatchHandle _batchHandle;
        private Stream _fileStream;
        private string _fileName;
        private string _contentType;
        private readonly BatchIdentifier _batchIdentifier;
        private const string BatchId = "batchId";
        private const string BusinessUnit = "ADDS-S100";
        private const string ConfigKey = "orchestrator:BusinessUnit";
        private const string ExpiryConfigKey = "orchestrator:Response:BatchExpiresIn";

        [SetUp]
        public void SetUp()
        {
            _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
            _configuration = A.Fake<IConfiguration>();
            _logger = A.Fake<ILogger<DefaultFileService>>();
            _correlationId = CorrelationId.From("correlationId");
            _cancellationToken = CancellationToken.None;
            _userIdentifier = new UserIdentifier() { Identity = "userId" };
            _batchHandle = new BatchHandle(BatchId);
            _fileStream = new MemoryStream([1, 2, 3]);
            _fileName = "file.zip";
            _contentType = "application/zip";
            A.CallTo(() => _configuration["orchestrator:Response:ExchangeSetExpiresIn"]).Returns("1.00:00:00");
           
            var businessUnitSection = A.Fake<IConfigurationSection>();
            A.CallTo(() => businessUnitSection.Value).Returns(BusinessUnit);
            A.CallTo(() => _configuration.GetSection(ConfigKey)).Returns(businessUnitSection);

            var expirySection = A.Fake<IConfigurationSection>();
            A.CallTo(() => expirySection.Value).Returns(TimeSpan.FromDays(1).ToString());
            A.CallTo(() => _configuration.GetSection(ExpiryConfigKey)).Returns(expirySection);

            _defaultFileService = new DefaultFileService(_fileShareReadWriteClient, _configuration, _logger);
        }

        [TestCase(ExchangeSetType.ProductNames)]
        [TestCase(ExchangeSetType.ProductVersions)]
        [TestCase(ExchangeSetType.UpdatesSince)]
        public async Task WhenCreateBatchAsyncWithCustomExchangeSet_ThenReturnsBatch(ExchangeSetType exchangeSetType)
        {
            var result = A.Fake<IResult<IBatchHandle>>();
            IBatchHandle? handle = _batchHandle;
            IError? error = null;
            A.CallTo(() => result.IsSuccess(out handle)).Returns(true);
            A.CallTo(() => result.IsFailure(out error, out handle)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._)).Returns(result);

            var batch = await _defaultFileService.CreateBatchAsync(_correlationId, exchangeSetType, _userIdentifier, _cancellationToken);

            Assert.That(batch.BatchId.Value, Is.EqualTo(BatchId));
            Assert.That(batch.BatchExpiryDateTime, Is.Not.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void WhenCreateBatchAsyncFails_ThenThrowsInvalidOperationException()
        {
            var result = A.Fake<IResult<IBatchHandle>>();
            IBatchHandle? handle = _batchHandle;
            var error = A.Fake<IError>();
            A.CallTo(() => result.IsSuccess(out handle)).Returns(false);
            A.CallTo(() => result.IsFailure(out error, out handle)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._)).Returns(result);

            Assert.That(async () => await _defaultFileService.CreateBatchAsync(_correlationId, ExchangeSetType.Complete, _userIdentifier, _cancellationToken),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task WhenCommitBatchAsyncSucceeds_ThenReturnsResponse()
        {
            var commitResponse = new CommitBatchResponse { Status = new CommitBatchStatus() };
            var result = A.Fake<IResult<CommitBatchResponse>>();
            A.CallTo(() => result.IsSuccess(out commitResponse)).Returns(true);
            IError? error = null;
            A.CallTo(() => result.IsFailure(out error, out commitResponse)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.CommitBatchAsync(_batchHandle, A<string>._, A<CancellationToken>._)).Returns(result);

            var response = await _defaultFileService.CommitBatchAsync(_batchHandle, _correlationId, _cancellationToken);

            Assert.That(response, Is.EqualTo(commitResponse));
        }

        [Test]
        public void WhenCommitBatchAsyncFails_ThenThrowsInvalidOperationException()
        {
            var result = A.Fake<IResult<CommitBatchResponse>>();
            CommitBatchResponse? commitResponse = null;
            var error = A.Fake<IError>();
            A.CallTo(() => result.IsSuccess(out commitResponse)).Returns(false);
            A.CallTo(() => result.IsFailure(out error, out commitResponse)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.CommitBatchAsync(_batchHandle, A<string>._, A<CancellationToken>._)).Returns(result);

            Assert.That(async () => await _defaultFileService.CommitBatchAsync(_batchHandle, _correlationId, _cancellationToken),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncReturnsEntries_ThenExcludesCurrentBatch()
        {
            var batchIdentifier = BatchIdentifier.From(BatchId);
            var entries = new List<BatchDetails>
            {
                new(batchId: BatchId),
                new(batchId: "otherBatchId")
            };
            var response = new BatchSearchResponse(entries: entries);
            var result = A.Fake<IResult<BatchSearchResponse>>();
            A.CallTo(() => result.IsSuccess(out response)).Returns(true);
            IError? error = null;
            A.CallTo(() => result.IsFailure(out error, out response)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.SearchAsync(A<string>._, A<int>._, A<int>._, A<string>._, A<CancellationToken>._)).Returns(result);

            var searchResult = await _defaultFileService.SearchCommittedBatchesExcludingCurrentAsync(
                batchIdentifier, _correlationId, _cancellationToken);

            Assert.That(searchResult.Entries.Count, Is.EqualTo(1));
            Assert.That(searchResult.Entries[0].BatchId, Is.EqualTo("otherBatchId"));
        }

        [Test]
        public void WhenSearchCommittedBatchesExcludingCurrentAsyncFails_ThenThrowsInvalidOperationException()
        {
            var response = new BatchSearchResponse();
            var result = A.Fake<IResult<BatchSearchResponse>>();
            var error = A.Fake<IError>();
            A.CallTo(() => result.IsSuccess(out response)).Returns(false);
            A.CallTo(() => result.IsFailure(out error, out response)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.SearchAsync(A<string>._, A<int>._, A<int>._, A<string>._, A<CancellationToken>._)).Returns(result);

            Assert.That(async () => await _defaultFileService.SearchCommittedBatchesExcludingCurrentAsync(
               _batchIdentifier, _correlationId, _cancellationToken),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncWithNoValidBatches_ThenReturnsTrue()
        {
            var batches = CreateBatchDetailsList("");

            var result = await _defaultFileService.SetExpiryDateAsync(batches, _correlationId, _cancellationToken);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncWithValidBatchesThen_ReturnsTrue()
        {
            var batches = CreateBatchDetailsList(BatchId);
            var setExpiryDateResponse = new SetExpiryDateResponse { IsExpiryDateSet = true };
            var expiryResult = A.Fake<IResult<SetExpiryDateResponse>>();
            A.CallTo(() => expiryResult.IsSuccess(out setExpiryDateResponse)).Returns(true);
            IError? error = null;
            A.CallTo(() => expiryResult.IsFailure(out error, out setExpiryDateResponse)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, A<BatchExpiryModel>._, A<string>._, A<CancellationToken>._)).Returns(expiryResult);

            var result = await _defaultFileService.SetExpiryDateAsync(batches, _correlationId, _cancellationToken);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncWithValidBatchesButFailure_ThenReturnsFalse()
        {
            var batches = CreateBatchDetailsList(BatchId);
            var expiryResult = A.Fake<IResult<SetExpiryDateResponse>>();
            SetExpiryDateResponse? dummy = null;
            var error = A.Fake<IError>();
            A.CallTo(() => expiryResult.IsSuccess(out dummy)).Returns(false);
            A.CallTo(() => expiryResult.IsFailure(out error, out dummy)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, A<BatchExpiryModel>._, A<string>._, A<CancellationToken>._)).Returns(expiryResult);

            var result = await _defaultFileService.SetExpiryDateAsync(batches, _correlationId, _cancellationToken);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenAddFileToBatchAsyncSucceeds_ThenReturnsAttributeList()
        {
            var response = new AddFileToBatchResponse
            {
                Attributes =
                [
                    new KeyValuePair<string, string>("Key1", "Value1"),
                    new KeyValuePair<string, string>("Key2", "Value2")
                ]
            };
            var result = A.Fake<IResult<AddFileToBatchResponse>>();
            A.CallTo(() => result.IsSuccess(out response)).Returns(true);
            IError? error = null;
            A.CallTo(() => result.IsFailure(out error, out response)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, A<string>._, A<CancellationToken>._)).Returns(result);

            var attributeList = await _defaultFileService.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, _correlationId, _cancellationToken);

            Assert.That(attributeList.Count, Is.EqualTo(2));
            Assert.That(attributeList.Any(a => a.Key == "Key1" && a.Value == "Value1"));
            Assert.That(attributeList.Any(a => a.Key == "Key2" && a.Value == "Value2"));
        }

        [Test]
        public void WhenAddFileToBatchAsyncFails_ThenThrowsInvalidOperationException()
        {
            var result = A.Fake<IResult<AddFileToBatchResponse>>();
            AddFileToBatchResponse? response = null;
            var error = A.Fake<IError>();
            A.CallTo(() => result.IsSuccess(out response)).Returns(false);
            A.CallTo(() => result.IsFailure(out error, out response)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, A<string>._, A<CancellationToken>._)).Returns(result);

            Assert.That(async () => await _defaultFileService.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, _correlationId, _cancellationToken),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task WhenAddFileToBatchAsyncWithNullResponse_ThenReturnsEmptyAttributeList()
        {
            AddFileToBatchResponse? response = null;
            var result = A.Fake<IResult<AddFileToBatchResponse>>();
            A.CallTo(() => result.IsSuccess(out response)).Returns(true);
            IError? error = null;
            A.CallTo(() => result.IsFailure(out error, out response)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, A<string>._, A<CancellationToken>._)).Returns(result);

            var attributeList = await _defaultFileService.AddFileToBatchAsync(_batchHandle, _fileStream, _fileName, _contentType, _correlationId, _cancellationToken);

            Assert.That(actual: attributeList, Is.Empty);
        }

        private static List<BatchDetails> CreateBatchDetailsList(string batchId)
        {
            return [new BatchDetails(batchId: batchId)];
        }
    }
}

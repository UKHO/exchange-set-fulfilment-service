using NUnit.Framework;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests
{
    [TestFixture]
    public class FileNameGeneratorServiceTests
    {
        private readonly JobId _defaultJobId = JobId.From("TEST123");

       [Test]
        public void WhenGeneratingFromTemplateWithNoParams_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test";

            var sut = new FileNameGeneratorService();
            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId);

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDefaultJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[jobid]";

            var sut = new FileNameGeneratorService();
            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId);

            Assert.That(result, Is.EqualTo("Test_TEST123"));
        }


        [Test]
        public void WhenGeneratingFromTemplateWithDateParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[date]";

            var sut = new FileNameGeneratorService();
            var date = new DateTime(2023, 10, 5);

            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId, date);

            Assert.That(result, Is.EqualTo($"Test_20231005"));
        }
    }
}

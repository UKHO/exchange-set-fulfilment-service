using NUnit.Framework;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Services;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Services
{
    [TestFixture]
    public class TemplateFileNameGeneratorServiceTests
    {
        private readonly JobId _defaultJobId = JobId.From("TEST123");

       [Test]
        public void WhenGeneratingFromTemplateWithNoParams_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test";

            var sut = new TemplateFileNameGeneratorService();
            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId);

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDefaultJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[jobid]";

            var sut = new TemplateFileNameGeneratorService();
            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId);

            Assert.That(result, Is.EqualTo("Test_TEST123"));
        }


        [Test]
        public void WhenGeneratingFromTemplateWithDateParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[date]";

            var sut = new TemplateFileNameGeneratorService();
            var date = new DateTime(2023, 10, 5);

            var result = sut.GenerateFileName(exchangeSetNameTemplate, _defaultJobId, date);

            Assert.That(result, Is.EqualTo($"Test_20231005"));
        }
    }
}

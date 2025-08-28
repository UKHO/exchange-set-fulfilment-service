using NUnit.Framework;
using UKHO.ADDS.EFS.Domain.Services.Factories;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Factories
{
    [TestFixture]
    public class FileNameGeneratorTests
    {
        private readonly JobId _defaultJobId = JobId.From("TEST123");

       [Test]
        public void WhenGeneratingFromTemplateWithNoParams_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var result = sut.GenerateFileName(_defaultJobId);

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDefaultJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[jobid]";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var result = sut.GenerateFileName(_defaultJobId);

            Assert.That(result, Is.EqualTo("Test_TEST123"));
        }


        [Test]
        public void WhenGeneratingFromTemplateWithDateParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[date]";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var date = new DateTime(2023, 10, 5);

            var result = sut.GenerateFileName(_defaultJobId, date);

            Assert.That(result, Is.EqualTo($"Test_20231005"));
        }
    }
}

using UKHO.ADDS.EFS.Builder.Common.Pipelines.Distribute;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Distribute
{
    [TestFixture]
    public class FileNameGeneratorTests
    {
        private const string DefaultJobId = "TEST123";

       [Test]
        public void WhenGeneratingFromTemplateWithNoParams_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var result = sut.GenerateFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDefaultJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[jobid]";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var result = sut.GenerateFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("Test_TEST123"));
        }


        [Test]
        public void WhenGeneratingFromTemplateWithDateParam_ThenReturnsTemplateVerbatim()
        {
            var exchangeSetNameTemplate = "Test_[date]";

            var sut = new FileNameGenerator(exchangeSetNameTemplate);
            var date = new DateTime(2023, 10, 5);

            var result = sut.GenerateFileName(DefaultJobId, date);

            Assert.That(result, Is.EqualTo($"Test_20231005"));
        }
    }
}

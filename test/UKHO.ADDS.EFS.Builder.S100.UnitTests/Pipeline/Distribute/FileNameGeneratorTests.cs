using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Distribute
{
    [TestFixture]
    public class FileNameGeneratorTests
    {
        private const string DefaultJobId = "TEST123";

       [Test]
        public void WhenGeneratingFromTemplateWithNoParams_ThenReturnsTemplateVerbatim()
        {
            var context = new ExchangeSetPipelineContext(null, null, null, null, null)
            {
                JobId = DefaultJobId,
                ExchangeSetNameTemplate = "Test"
            };

            var sut = new FileNameGenerator(context);
            var result = sut.GenerateFileName();

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDefaultJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var context = new ExchangeSetPipelineContext(null, null, null, null, null)
            {
                JobId = DefaultJobId,
                ExchangeSetNameTemplate = "Test_[jobid]"
            };

            var sut = new FileNameGenerator(context);
            var result = sut.GenerateFileName();

            Assert.That(result, Is.EqualTo("Test_TEST123"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithOverrideJobIdParam_ThenReturnsTemplateVerbatim()
        {
            var context = new ExchangeSetPipelineContext(null, null, null, null, null)
            {
                JobId = DefaultJobId,
                ExchangeSetNameTemplate = "Test_[jobid]"
            };

            var sut = new FileNameGenerator(context);
            var result = sut.GenerateFileName(jobId: "OVERRIDE");

            Assert.That(result, Is.EqualTo("Test_OVERRIDE"));
        }

        [Test]
        public void WhenGeneratingFromTemplateWithDateParam_ThenReturnsTemplateVerbatim()
        {
            var context = new ExchangeSetPipelineContext(null, null, null, null, null)
            {
                JobId = DefaultJobId,
                ExchangeSetNameTemplate = "Test_[date]"
            };

            var sut = new FileNameGenerator(context);
            var date = new DateTime(2023, 10, 5);

            var result = sut.GenerateFileName(date: date);

            Assert.That(result, Is.EqualTo($"Test_20231005"));
        }
    }
}

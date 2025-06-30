using UKHO.ADDS.EFS.Builder.S100.Services;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Services
{
    [TestFixture]
    internal class FileNameGeneratorTest
    {
        private const string DefaultJobId = "TEST123";
        private string? _originalEnvironment;

        [SetUp]
        public void Setup()
        {
            _originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }

        [Test]
        public void WhenGetExchangeSetFileNameCalledWithValidJobIdInLowerEnvironment_ThenReturnsFileNameWithJobId()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var result = FileNameGenerator.GetExchangeSetFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("V01X01_TEST123.zip"));
        }

        [Test]
        public void WhenGetExchangeSetFileNameCalledInHigherEnvironment_ThenReturnsStandardFileName()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            var result = FileNameGenerator.GetExchangeSetFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("V01X01.zip"));
        }

        [Test]
        public void WhenGetExchangeSetFileNameCalledWithNullJobId_ThenReturnsStandardFileName()
        {
            string jobId = null;
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var result = FileNameGenerator.GetExchangeSetFileName(jobId);

            Assert.That(result, Is.EqualTo("V01X01.zip"));
        }

        [Test]
        public void WhenGetExchangeSetFileNameCalledWithEmptyJobId_ThenReturnsStandardFileName()
        {
            var jobId = string.Empty;
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var result = FileNameGenerator.GetExchangeSetFileName(jobId);

            Assert.That(result, Is.EqualTo("V01X01.zip"));
        }

        [Test]
        public void WhenGetExchangeSetFileNameCalledWithoutEnvironment_ThenDefaultsToDevelopment()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

            var result = FileNameGenerator.GetExchangeSetFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("V01X01_TEST123.zip"));
        }

        [TestCase("local")]
        [TestCase("Local")]
        [TestCase("LOCAL")]
        [TestCase("Development")]
        [TestCase("development")]
        [TestCase("Dev")]
        [TestCase("dev")]
        public void WhenGetExchangeSetFileNameCalledInAnyLowerEnvironmentCase_ThenReturnsFileNameWithJobId(string environmentName)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

            var result = FileNameGenerator.GetExchangeSetFileName(DefaultJobId);

            Assert.That(result, Is.EqualTo("V01X01_TEST123.zip"));
        }
    }
}

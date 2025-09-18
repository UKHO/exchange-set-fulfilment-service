using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public abstract class TestBase
    {
        protected readonly StartupFixture startup;
        protected readonly ITestOutputHelper _output;

        protected TestBase(StartupFixture startup, ITestOutputHelper output)
        {
            this.startup = startup;
            _output = output;
        }
    }
}

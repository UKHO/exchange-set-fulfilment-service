using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public abstract class TestBase : IDisposable
    {
        protected readonly StartupFixture startup;
        protected readonly ITestOutputHelper _output;
        private readonly IDisposable _outputScope;

        protected TestBase(StartupFixture startup, ITestOutputHelper output)
        {
            this.startup = startup;
            _output = output;

            // Set the current test output context
            // TestOutputContext.Current = output;

            // Begin a scope that sets TestOutputContext.Current and restores the previous value on dispose
            _outputScope = TestOutputContext.BeginScope(output);
        }

        // Implement IDisposable to automatically clean up when tests complete
        public virtual void Dispose()
        {
            //TestOutputContext.Clear();
            _outputScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

using Microsoft.VisualStudio.TestPlatform.Utilities;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Framework
{
    public abstract class FunctionalTestBase(StartupFixture startUp, ITestOutputHelper output) : IDisposable
    {
        protected readonly StartupFixture startup = startUp;
        protected readonly ITestOutputHelper _output = output;

        // Begin a scope that sets TestOutput.Current and restores the previous value on dispose
        private readonly IDisposable _outputScope = TestOutput.BeginScope(output);

        // Implement IDisposable to automatically clean up when tests complete
        public virtual void Dispose()
        {
            _outputScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure
{
    public abstract class OrchestratorTest
    {
        protected ITestOutputHelper Output { get; }

        protected OrchestratorTest(ITestOutputHelper output)
        {
            Output = output;
        }

        protected GivenStep<T> Given<T>(string title, Func<T> arrange)
        {
            WriteSection("GIVEN", title);
            var result = arrange();
            return new GivenStep<T>(result, Output);
        }

        protected GivenStep<T> Given<T>(string title, Func<Task<T>> arrange)
        {
            WriteSection("GIVEN", title);
            var result = arrange().GetAwaiter().GetResult();
            return new GivenStep<T>(result, Output);
        }

        private void WriteSection(string section, string title)
        {
            Output?.WriteLine($"=== {section}: {title}");
        }
    }
}

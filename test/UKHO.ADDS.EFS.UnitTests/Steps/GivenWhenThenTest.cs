using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace UKHO.ADDS.EFS
{
    public abstract class GivenWhenThenTest
    {
        protected ITestOutputHelper Output { get; }

        protected GivenWhenThenTest(ITestOutputHelper output)
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
            Output?.WriteLine($"{section}:       {title}");
        }
    }
}

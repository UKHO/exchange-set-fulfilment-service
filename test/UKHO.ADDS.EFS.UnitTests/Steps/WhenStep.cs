using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace UKHO.ADDS.EFS
{
    public class WhenStep<T>
    {
        private readonly T _sut;
        private readonly ITestOutputHelper _output;

        internal WhenStep(T sut, ITestOutputHelper output)
        {
            _sut = sut;
            _output = output;
        }

        public Task Then(string title, Action<T> assert)
        {
            WriteSection("THEN", title);

            assert(_sut);
            return Task.CompletedTask;
        }

        public async Task Then(string title, Func<T, Task> assert)
        {
            WriteSection("THEN", title);

            await assert(_sut);
        }

        private void WriteSection(string section, string title)
        {
            _output?.WriteLine($"    => {section}: {title}");
        }
    }
}

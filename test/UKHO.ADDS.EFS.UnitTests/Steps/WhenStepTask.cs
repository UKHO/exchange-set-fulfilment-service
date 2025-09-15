using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace UKHO.ADDS.EFS
{
    public class WhenStepTask<T>
    {
        private readonly T _sut;
        private readonly Task _actTask;
        private readonly ITestOutputHelper _output;

        internal WhenStepTask(T sut, Task actTask, ITestOutputHelper output)
        {
            _sut = sut;
            _actTask = actTask;
            _output = output;
        }

        public async Task Then(string title, Action<T> assert)
        {
            await _actTask;

            WriteSection("THEN", title);
            assert(_sut);
        }

        public async Task Then(string title, Func<T, Task> assert)
        {
            await _actTask;

            WriteSection("THEN", title);
            await assert(_sut);
        }

        private void WriteSection(string section, string title)
        {
            _output?.WriteLine($"    => {section}: {title}");
        }
    }
}

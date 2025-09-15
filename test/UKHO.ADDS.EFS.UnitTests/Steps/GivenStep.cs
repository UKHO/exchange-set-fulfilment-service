using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace UKHO.ADDS.EFS
{
    public class GivenStep<T>
    {
        private readonly T _sut;
        private readonly ITestOutputHelper _output;

        internal GivenStep(T sut, ITestOutputHelper output)
        {
            _sut = sut;
            _output = output;
        }

        public WhenStep<T> When(string title, Action<T> act)
        {
            WriteSection("WHEN", title);

            act(_sut);
            return new WhenStep<T>(_sut, _output);
        }

        public WhenStepTask<T> When(string title, Func<T, Task> act)
        {
            WriteSection("WHEN", title);

            var task = act(_sut);
            return new WhenStepTask<T>(_sut, task, _output);
        }

        private void WriteSection(string section, string title)
        {
            _output?.WriteLine($"  => {section}:   {title}");
        }
    }
}

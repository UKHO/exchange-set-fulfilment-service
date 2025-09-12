namespace UKHO.ADDS.EFS.Infrastructure.Builders.Pipelines
{
    internal static class IncrementingCounter
    {
        private static int _counter;

        static IncrementingCounter() => _counter = 0;

        public static string GetNext()
        {
            var current = Interlocked.Increment(ref _counter);
            return current.ToString("D4");
        }
    }
}

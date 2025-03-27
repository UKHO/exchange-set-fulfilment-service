namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    internal static class IncrementingCounter
    {
        private static int _counter = 0;

        static IncrementingCounter()
        {
            _counter = 0;
        }

        public static string GetNext()
        {
            var current = Interlocked.Increment(ref _counter);
            return current.ToString("D4");
        }
    }
}

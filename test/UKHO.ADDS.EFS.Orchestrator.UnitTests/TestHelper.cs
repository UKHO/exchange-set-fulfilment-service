using Azure;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests
{
    public static class TestHelper
    {
        public static AsyncPageable<T> CreateAsyncPageable<T>(IEnumerable<T> items)
        {
            return AsyncPageable<T>.FromPages(new[] { Page<T>.FromValues(items.ToList(), null, null) });
        }
    }
}

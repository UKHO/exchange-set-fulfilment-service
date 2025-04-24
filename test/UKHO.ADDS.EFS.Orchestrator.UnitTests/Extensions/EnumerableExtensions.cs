using Azure;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Extensions
{
    internal static class EnumerableExtensions
    {
        public static AsyncPageable<T> CreateAsyncPageable<T>(this IEnumerable<T> items) where T : notnull
        {
            return AsyncPageable<T>.FromPages(new[] { Page<T>.FromValues(items.ToList(), null, null!) });
        }
    }
}

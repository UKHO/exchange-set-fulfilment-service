using System.Collections;

namespace UKHO.Infrastructure.Pipelines.Extensions
{
    internal static class ListExtensions
    {
        public static bool SafeAny(this IList list) => list != null && list.Count > 0;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard
{
    public static class DocsExtensions
    {
        public static IServiceCollection AddDashboard(this IServiceCollection services) =>
            services
                .AddTabler(options => { options.AssemblyScanFilter = () => [typeof(DocsExtensions).Assembly]; })
                .AddSingleton<AppService>()
                .AddScoped<ICodeSnippetService, LocalSnippetService>();

        internal static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new T[size];
                }

                bucket[count++] = item;

                if (count != size)
                {
                    continue;
                }

                yield return bucket.Select(x => x);

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                Array.Resize(ref bucket, count);
                yield return bucket.Select(x => x);
            }
        }
    }
}

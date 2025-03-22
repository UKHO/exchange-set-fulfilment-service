using Microsoft.Extensions.DependencyInjection;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddOrchestrator(this IServiceCollection collection)
        {
            // Add services here

            return collection;
        }

    }
}

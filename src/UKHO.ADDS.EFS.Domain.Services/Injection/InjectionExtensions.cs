using Microsoft.Extensions.DependencyInjection;

namespace UKHO.ADDS.EFS.Domain.Services.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection collection)
        {

            return collection;
        }
    }
}

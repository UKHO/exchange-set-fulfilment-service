using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.EFS.Domain.User;

namespace UKHO.ADDS.EFS.Domain.Services.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection collection)
        {
            collection.AddScoped<UserIdentifier>();

            return collection;
        }
    }
}

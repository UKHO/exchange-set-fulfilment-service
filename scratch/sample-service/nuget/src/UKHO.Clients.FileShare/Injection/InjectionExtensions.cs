using Microsoft.Extensions.DependencyInjection;
using UKHO.Clients.Common.Configuration;

namespace UKHO.Clients.FileShare.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFileShare(this IServiceCollection collection, ClientConfiguration configuration)
        {
            collection.AddSingleton<IFileShareClientFactory, FileShareClientFactory>();

            return collection;
        }
    }
}

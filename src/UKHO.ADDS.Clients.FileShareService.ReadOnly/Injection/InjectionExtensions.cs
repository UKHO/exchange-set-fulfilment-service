using Microsoft.Extensions.DependencyInjection;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddReadOnlyFileShareClient(this IServiceCollection collection)
        {
            collection.AddTransient<IFileShareReadOnlyClientFactory, FileShareReadOnlyClientFactory>();

            return collection;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddReadWriteFileShareClient(this IServiceCollection collection)
        {
            collection.AddTransient<IFileShareReadOnlyClientFactory, FileShareReadOnlyClientFactory>();

            return collection;
        }
    }
}

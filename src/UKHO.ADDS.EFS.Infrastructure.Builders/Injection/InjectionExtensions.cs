using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Services;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddBuilderInfrastructure(this IServiceCollection collection)
        {
            collection.AddTransient<IFileNameGeneratorService, TemplateFileNameGeneratorService>();

            return collection;
        }
    }
}

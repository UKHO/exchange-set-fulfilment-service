using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Infrastructure.Services;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S100;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S57;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S63;

namespace UKHO.ADDS.EFS.Infrastructure.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection collection)
        {
            collection.AddSingleton<IRepository<S100Build>, S100BuildRepository>();
            collection.AddSingleton<IRepository<S63Build>, S63BuildRepository>();
            collection.AddSingleton<IRepository<S57Build>, S57BuildRepository>();
            
            collection.AddSingleton<IRepository<DataStandardTimestamp>, DataStandardTimestampRepository>();
            collection.AddSingleton<IRepository<Job>, JobRepository>();
            collection.AddSingleton<IRepository<BuildMemento>, BuildMementoRepository>();

            collection.AddTransient<IFileNameGeneratorService, TemplateFileNameGeneratorService>();

            collection.AddSingleton<IHashingService, BlakeHashingService>();
            collection.AddSingleton<IStorageService, StorageService>();
            collection.AddSingleton<ITimestampService, TimestampService>();

            return collection;
        }
    }
}

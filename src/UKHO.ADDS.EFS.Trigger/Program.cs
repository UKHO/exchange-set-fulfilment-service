using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace UKHO.ADDS.EFS.Trigger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = FunctionsApplication.CreateBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddHttpClient();

            builder.Build().Run();
        }
    }
}

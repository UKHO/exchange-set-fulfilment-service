
using AzureKeyVaultEmulator.Aspire.Client;
using Microsoft.Extensions.Azure;
using Radzen;
using Scalar.AspNetCore;
using UKHO.ADDS.Configuration.Dashboard;
using UKHO.ADDS.Configuration.Dashboard.Services;
using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.Configuration.Services;

namespace UKHO.ADDS.Configuration
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();
            builder.Services.AddAuthorization();

            builder.Services.AddOpenApi();

            builder.AddAzureTableClient(WellKnownConfigurationName.ConfigurationServiceTableStorageName);

            var vaultEndpoint = builder.Configuration.GetConnectionString(WellKnownConfigurationName.ConfigurationServiceKeyVaultName) ?? string.Empty;

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddAzureKeyVaultEmulator(vaultEndpoint, true, certificates: false, keys: true);
            }
            else
            {
                builder.Services.AddAzureClients(client =>
                {
                    var vaultUri = new Uri(vaultEndpoint);

                    client.AddSecretClient(vaultUri);
                    client.AddKeyClient(vaultUri);
                });
            }

            builder.Services.AddGrpc();

            builder.Services.AddSingleton<ConfigurationStore>();
            builder.Services.AddSingleton<ConfigurationWriter>();
            builder.Services.AddSingleton<ConfigurationReader>();

            builder.Services.AddSingleton<ConfigurationService>();

            builder.WebHost.UseStaticWebAssets();

            builder.Services.AddRazorPages();
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();

            builder.Services.AddScoped<DashboardPageService>();
            builder.Services.AddSingleton<DashboardService>();
            builder.Services.AddLocalization();

            var app = builder.Build();

            var addsEnvironment = AddsEnvironment.Parse(app.Configuration[WellKnownConfigurationName.AddsEnvironmentName]!);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();

            app.MapRazorPages();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            var configurationService = app.Services.GetRequiredService<ConfigurationStore>();
            await configurationService.InitialiseAsync(addsEnvironment);

            app.MapGet("/configuration", (HttpContext httpContext, ConfigurationStore configurationService) =>
            {
                return configurationService.Configuration;
            })
            .WithName("GetConfiguration");

            app.MapWhen(context => context.Request.ContentType?.StartsWith("application/grpc") == true, grpcApp =>
            {
                grpcApp.UseRouting();
                grpcApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<ConfigurationService>();
                });
            });

            //app.MapGrpcService<ConfigurationService>();

            await app.RunAsync();
        }
    }
}

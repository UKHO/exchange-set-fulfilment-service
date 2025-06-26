
using AzureKeyVaultEmulator.Aspire.Client;
using Microsoft.Extensions.Azure;
using Radzen;
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

            builder.Services.AddSingleton<ConfigurationService>();
            builder.Services.AddSingleton<ConfigurationWriter>();
            builder.Services.AddSingleton<ConfigurationReader>();

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
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();

            app.MapRazorPages();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            var configurationService = app.Services.GetRequiredService<ConfigurationService>();
            await configurationService.InitialiseAsync(addsEnvironment);

            //app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            //{
            //    var forecast = Enumerable.Range(1, 5).Select(index =>
            //        new WeatherForecast
            //        {
            //            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            //            TemperatureC = Random.Shared.Next(-20, 55),
            //            Summary = summaries[Random.Shared.Next(summaries.Length)]
            //        })
            //        .ToArray();
            //    return forecast;
            //})
            //.WithName("GetWeatherForecast");

            await app.RunAsync();
        }
    }
}

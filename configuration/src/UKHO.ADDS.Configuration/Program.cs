
using AzureKeyVaultEmulator.Aspire.Client;
using Microsoft.Extensions.Azure;
using UKHO.ADDS.Configuration.Schema;

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

            builder.Services.AddSingleton<ConfigurationWriter>();

            var app = builder.Build();

            var addsEnvironment = AddsEnvironment.Parse(app.Configuration[WellKnownConfigurationName.AddsEnvironmentName]!);

            if (AddsEnvironment.Local.Equals(addsEnvironment))
            {
                await RunImportAsync(app, addsEnvironment);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

            app.Run();
        }

        private static async Task RunImportAsync(WebApplication app, AddsEnvironment environment)
        {
            var configFilePath = app.Configuration[WellKnownConfigurationName.ConfigurationFilePath]!;
            var json = await File.ReadAllTextAsync(configFilePath);

            var configWriter = app.Services.GetRequiredService<ConfigurationWriter>();
            await configWriter.WriteConfigurationAsync(environment, json);
        }
    }
}

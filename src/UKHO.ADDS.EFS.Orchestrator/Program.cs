using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TabBlazor.QuickTable.EntityFramework;
using UKHO.ADDS.EFS.Orchestrator.Dashboard;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Injection;

namespace UKHO.ADDS.EFS.Orchestrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder.Services);

            builder.AddServiceDefaults();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.UseAuthorization();

            MapApis(app);

//            var ts = new TestService();
//            ts.TestMethod();

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthorization();
            services.AddOpenApi();

            services.AddScoped<IDataService, LocalDataService>();
            services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite("Data Source=app.db"));
            services.AddQuickTableEntityFrameworkAdapter();
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddOrchestrator();
            services.AddDashboard();
        }

        private static void MapApis(WebApplication app)
        {
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
                {
                    var forecast = Enumerable.Range(1, 5).Select(index =>
                            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)), TemperatureC = Random.Shared.Next(-20, 55), Summary = summaries[Random.Shared.Next(summaries.Length)] })
                        .ToArray();
                    return forecast;
                })
                .WithName("GetWeatherForecast");
        }
    }
}

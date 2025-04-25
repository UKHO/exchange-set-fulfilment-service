using Scalar.AspNetCore;
using Serilog;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Middleware;

namespace UKHO.ADDS.EFS.Orchestrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting the EFS Orchestrator");

                var builder = WebApplication.CreateBuilder(args);

                var oltpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!;

                builder.Services.AddSerilog((services, lc) => lc
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.OpenTelemetry(o =>
                    {
                        o.Endpoint = oltpEndpoint;
                    })
                    .WriteTo.Console());

                builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

                builder.AddServiceDefaults()
                    .AddOrchestratorServices();

                var app = builder.Build();

                app.UseSerilogRequestLogging();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
                }

                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseAuthorization();

                var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

                app.RegisterJobsApi(loggerFactory);
                app.RegisterStatusApi(loggerFactory);
                app.RegisterRequestsApi(loggerFactory);

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "EFS Orchestrator terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

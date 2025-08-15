using Aspire.Hosting;
using UKHO.ADDS.EFS.Configuration.Namespaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;  //rhz:
using System.Collections.Concurrent;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class TestBase : IAsyncLifetime
    {
        protected DistributedApplication? App;
        public static string? ProjectDirectory;

        public TestBase()
        {
            ProjectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        }


        // rhz: In-memory logger provider for capturing logs during tests. Start
        public class InMemoryLoggerProvider : ILoggerProvider
        {
            private readonly InMemoryLogger _logger = new();

            public ILogger CreateLogger(string categoryName) => _logger;

            public void Dispose() { }

            public IReadOnlyCollection<LogEntry> GetLogs() => _logger.Logs;
        }

        public class InMemoryLogger : ILogger
        {
            public ConcurrentBag<LogEntry> Logs { get; } = new();

            public IDisposable BeginScope<TState>(TState state) => NullLogger.Instance.BeginScope(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Logs.Add(new LogEntry
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    Message = formatter(state, exception),
                    Exception = exception
                });
            }
        }

        public record LogEntry
        {
            public LogLevel LogLevel { get; init; }
            public EventId EventId { get; init; }
            public string Message { get; init; } = string.Empty;
            public Exception? Exception { get; init; }
        }

        // Usage in TestBase
        public InMemoryLoggerProvider LoggerProvider { get; private set; } = new();
        // rhz: In-memory logger provider for capturing logs during tests. End

        public async Task InitializeAsync()
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UKHO_ADDS_EFS_LocalHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            // rhz: Add in-memory logger provider
            appHost.Services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(LoggerProvider);
            });

            App = await appHost.BuildAsync();

            var resourceNotificationService = App.Services.GetRequiredService<ResourceNotificationService>();
            await App.StartAsync();
            await resourceNotificationService
                .WaitForResourceAsync(ProcessNames.OrchestratorService, KnownResourceStates.Running)
                .WaitAsync(TimeSpan.FromSeconds(30));
        }

        // rhz: Example: Access captured logs in your tests
        // rhz: var logs = LoggerProvider.GetLogs();

        public async Task DisposeAsync()
        {
            if (App != null)
            {
                await App.StopAsync();
                await App.DisposeAsync();
            }

            //Clean up temporary files and directories
            var outDir = Path.Combine(ProjectDirectory!, "out");

            if (Directory.Exists(outDir))
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);

        }
    }
}

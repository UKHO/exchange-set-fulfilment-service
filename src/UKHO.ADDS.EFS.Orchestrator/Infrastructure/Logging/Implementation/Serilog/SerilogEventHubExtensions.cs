using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog
{
    /// <summary>
    /// Extends Serilog with methods to add Azure Event Hub sinks
    /// </summary>
    public static class SerilogEventHubExtensions
    {
        /// <summary>
        /// Adds an Azure Event Hub sink to the Serilog configuration
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog logger configuration</param>
        /// <param name="configureOptions">A callback to configure the EventHub sink options</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required to write an event to the sink</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        public static LoggerConfiguration EventHub(this LoggerSinkConfiguration loggerConfiguration,
            Action<EventHubSinkOptions> configureOptions, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new EventHubSinkOptions();
            configureOptions(options);

            // Create the EventHubClientWrapper based on options
            IEventHubClientWrapper clientWrapper;
            if (IsUsingManagedIdentity(options))
            {
                ValidateManagedIdentityOptions(options);
                clientWrapper = new EventHubClientWrapper(options.EventHubFullyQualifiedNamespace, options.EventHubEntityPath,
                    options.TokenCredential);
            }
            else
            {
                ValidateConnectionStringOptions(options);
                clientWrapper = new EventHubClientWrapper(options.EventHubConnectionString, options.EventHubEntityPath);
            }

            // Validate connection if requested
            if (options.EnableConnectionValidation)
            {
                clientWrapper.ValidateConnection();
            }

            // Create the EventHubLog
            var eventHubLog = new EventHubLog(clientWrapper, options.CustomLogSerializerConverters);

            // Create the batched sink
            var batchedSink = new EventHubBatchedSink(eventHubLog, options.Environment, options.System, options.Service,
                options.AdditionalValuesProvider);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = options.BatchSizeLimit,
                Period = options.Period,
                QueueLimit = options.QueueSizeLimit
            };

            return loggerConfiguration.Sink(new PeriodicBatchingSink(batchedSink, batchingOptions),
                restrictedToMinimumLevel);
        }


        private static bool IsUsingManagedIdentity(EventHubSinkOptions options)
        {
            return !string.IsNullOrEmpty(options.EventHubFullyQualifiedNamespace);
        }

        private static void ValidateManagedIdentityOptions(EventHubSinkOptions options)
        {
            var errors = new List<string>();

            if (options.TokenCredential == null)
            {
                errors.Add(nameof(options.TokenCredential));
            }

            ValidateCommonOptions(options, errors);

            if (errors.Count > 0)
            {
                throw new ArgumentException($"Parameters {string.Join(",", errors)} must be set to a valid value.", string.Join(",", errors));
            }

        }

        private static void ValidateConnectionStringOptions(EventHubSinkOptions options)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(options.EventHubConnectionString))
            {
                errors.Add(nameof(options.EventHubConnectionString));
            }

            ValidateCommonOptions(options, errors);

            if (errors.Count > 0)
            {
                throw new ArgumentException($"Parameters {string.Join(",", errors)} must be set to a valid value.",
                    string.Join(",", errors));
            }

        }

        private static void ValidateCommonOptions(EventHubSinkOptions options, List<string> errors)
        {
            if (string.IsNullOrEmpty(options.EventHubEntityPath))
            {
                errors.Add(nameof(options.EventHubEntityPath));
            }

            if (string.IsNullOrEmpty(options.Environment))
            {
                errors.Add(nameof(options.Environment));
            }

            if (string.IsNullOrEmpty(options.System))
            {
                errors.Add(nameof(options.System));
            }

            if (string.IsNullOrEmpty(options.Service))
            {
                errors.Add(nameof(options.Service));
            }

            if (options.AdditionalValuesProvider == null)
            {
                errors.Add(nameof(options.AdditionalValuesProvider));
            }

            if (options.CustomLogSerializerConverters == null)
            {
                errors.Add(nameof(options.CustomLogSerializerConverters));
            }
        }
    }
}

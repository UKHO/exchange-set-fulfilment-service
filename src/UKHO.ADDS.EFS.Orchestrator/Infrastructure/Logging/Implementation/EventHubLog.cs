// British Crown Copyright © 2023,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventHubs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    internal class EventHubLog : IEventHubLog
    {
        private const int LogSerializationExceptionEventId = 7437;
        private const string LogSerializationExceptionEventName = "LogSerializationException";

        private IEventHubClientWrapper eventHubClientWrapper;

        private readonly JsonSerializerOptions _settings;
        private readonly JsonSerializerOptions _errorSettings;

        private readonly AzureStorageBlobContainerBuilder azureStorageBlobContainerBuilder;
        private bool disposed;

        public EventHubLog(IEventHubClientWrapper eventHubClientWrapper, IEnumerable<JsonConverter> customConverters)
        {
            this.eventHubClientWrapper = eventHubClientWrapper;

            // Common options
            var settings = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Add custom converters if needed
            foreach (var converter in customConverters)
            {
                settings.Converters.Add(converter);
            }

            var errorSettings = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Add custom converters if needed
            foreach (var converter in customConverters)
            {
                errorSettings.Converters.Add(converter);
            }


            azureStorageBlobContainerBuilder = eventHubClientWrapper.AzureStorageBlobContainerBuilder;
        }

        public async void Log(LogEntry logEntry)
        {
            try
            {
                string jsonLogEntry;
                try
                {
                    jsonLogEntry = JsonCodec.Encode(logEntry, _settings);
                }
                catch (Exception e)
                {
                    logEntry = new LogEntry
                    {
                        Exception = e,
                        Level = "Warning",
                        MessageTemplate = "Log Serialization failed with exception",
                        Timestamp = DateTime.UtcNow,
                        EventId = new EventId(LogSerializationExceptionEventId, LogSerializationExceptionEventName)
                    };
                    jsonLogEntry = JsonCodec.Encode(logEntry, _errorSettings);
                }

                var azureStorageLoggingCheckResult = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(jsonLogEntry, 1);

                switch (azureStorageLoggingCheckResult)
                {
                    case AzureStorageLoggingCheckResult.LogWarningNoStorage:
                        jsonLogEntry = logEntry.ToLongMessageWarning(_settings);
                        break;
                    case AzureStorageLoggingCheckResult.LogWarningAndStoreMessage:
                        var azureLogger = new AzureStorageEventLogger(azureStorageBlobContainerBuilder.BlobContainerClient);
                        var blobFullName = azureLogger.GenerateBlobFullName(
                                                                               new AzureStorageBlobFullNameModel(azureLogger.GenerateServiceName(
                                                                                                                  logEntry.LogProperties.GetLogEntryPropertyValue("_Service"),
                                                                                                                  logEntry.LogProperties.GetLogEntryPropertyValue("_Environment")),
                                                                                                                 azureLogger.GeneratePathForErrorBlob(logEntry.Timestamp),
                                                                                                                 azureLogger.GenerateErrorBlobName()));
                        var azureStorageModel = new AzureStorageEventModel(blobFullName, jsonLogEntry);
                        var result = await azureLogger.StoreLogFileAsync(azureStorageModel);

                        jsonLogEntry = result.ToJsonLogEntryString(azureStorageBlobContainerBuilder.AzureStorageLogProviderOptions, logEntry, _settings);
                        break;
                }

                await eventHubClientWrapper.SendAsync(new EventData(Encoding.UTF8.GetBytes(jsonLogEntry)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                eventHubClientWrapper?.Dispose();
                eventHubClientWrapper = null;
            }

            disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EventHubLog()
        {
            Dispose(false);
        }
    }
}

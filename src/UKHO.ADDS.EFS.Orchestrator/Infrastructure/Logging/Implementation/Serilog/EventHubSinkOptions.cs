// British Crown Copyright © 2024,
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

using System.Text.Json.Serialization;
using Serilog.Formatting;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog
{
    /// <summary>
    /// Configuration options for the EventHub Serilog sink
    /// </summary>
    public class EventHubSinkOptions
    {
        private const string HashMachineName = "#MachineName";
        private string _nodeName = HashMachineName;

        /// <summary>
        /// The connection string for Event Hub
        /// </summary>
        public string EventHubConnectionString { get; set; }

        /// <summary>
        /// The entity path for Event Hub
        /// </summary>
        public string EventHubEntityPath { get; set; }

        /// <summary>
        /// The fully qualified Event Hubs namespace to connect to (for Managed Identity)
        /// </summary>
        public string EventHubFullyQualifiedNamespace { get; set; }

        /// <summary>
        /// The TokenCredential for Managed Identity authentication
        /// </summary>
        public Azure.Core.TokenCredential TokenCredential { get; set; }

        /// <summary>
        /// The environment value that will be included in all log entries
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// The system value that will be included in all log entries
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// The service value that will be included in all log entries
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// The node name that will be included in all log entries
        /// </summary>
        public string NodeName
        {
            get
            {
                return _nodeName == HashMachineName ? global::System.Environment.MachineName : _nodeName;
            }
            set { _nodeName = value; }
        }

        /// <summary>
        /// A callback to add additional values to each log entry
        /// </summary>
        public Action<IDictionary<string, object>> AdditionalValuesProvider { get; set; } = d => { };

        /// <summary>
        /// The options for Azure Storage logging
        /// </summary>
        //public AzureStorageEventLogging.Models.AzureStorageLogProviderOptions AzureStorageLogProviderOptions { get; set; }

        /// <summary>
        /// Enable connection validation with Event Hub
        /// </summary>
        public bool EnableConnectionValidation { get; set; } = false;

        /// <summary>
        /// Custom JSON converters to use when serializing log entries
        /// </summary>
        public IEnumerable<JsonConverter> CustomLogSerializerConverters { get; set; } = new List<JsonConverter>();

        /// <summary>
        /// The text formatter to use for formatting log events (defaults to JsonFormatter)
        /// </summary>
        public ITextFormatter Formatter { get; set; }

        /// <summary>
        /// The batch size when using the periodic batching sink (defaults to 100)
        /// </summary>
        public int BatchSizeLimit { get; set; } = 100;

        /// <summary>
        /// The time to wait between checking for event batches (defaults to 5 seconds)
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The maximum number of events to hold in memory (defaults to 10000)
        /// </summary>
        public int QueueSizeLimit { get; set; } = 10000;
    }
}

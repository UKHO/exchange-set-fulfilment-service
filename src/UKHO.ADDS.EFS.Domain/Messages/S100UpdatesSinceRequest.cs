using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Request model for S100 updates since datetime endpoint
    /// </summary>
    internal class S100UpdatesSinceRequest
    {
        /// <summary>
        /// The date and time from which changes are requested
        /// </summary>
        public required DateTime SinceDateTime { get; set; }

        [JsonIgnore]
        public string? CallbackUri { get; set; }

        [JsonIgnore]
        public string? ProductIdentifier { get; set; }
    }
}

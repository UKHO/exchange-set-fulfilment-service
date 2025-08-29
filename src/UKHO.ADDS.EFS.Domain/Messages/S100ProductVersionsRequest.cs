using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Request model for S100 product versions endpoint
    /// </summary>
    internal class S100ProductVersionsRequest
    {
        /// <summary>
        /// List of S100 product versions to request
        /// </summary>
        public required List<S100ProductVersion> ProductVersions { get; set; }

        /// <summary>
        /// Ignore this property from Request body
        /// </summary>
        [JsonIgnore]
        public string? CallbackUri { get; init; }
    }
}

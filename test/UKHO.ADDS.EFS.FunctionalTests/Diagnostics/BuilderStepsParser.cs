using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.FunctionalTests.Diagnostics
{
    /// <summary>
    /// Analyzer for builder steps from JSON responses
    /// </summary>
    public class BuilderStepsAnalyzer
    {
        public class BuilderResponse
        {
            [JsonPropertyName("jobId")]
            public string JobId { get; set; } = string.Empty;

            [JsonPropertyName("builderSteps")]
            public List<BuilderStep> BuilderSteps { get; set; } = new List<BuilderStep>();

            [JsonPropertyName("builderExitCode")]
            public string BuilderExitCode { get; set; } = string.Empty;

            // Calculate total elapsed time
            public double TotalElapsedMilliseconds => BuilderSteps.Sum(step => step.ElapsedMilliseconds);

            // Calculate total elapsed time in seconds
            public double TotalElapsedSeconds => TotalElapsedMilliseconds / 1000;
        }

        public class BuilderStep
        {
            [JsonPropertyName("sequence")]
            public string Sequence { get; set; } = string.Empty;

            [JsonPropertyName("nodeId")]
            public string NodeId { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("elapsedMilliseconds")]
            public double ElapsedMilliseconds { get; set; }
        }

        /// <summary>
        /// Parse JSON into a typed BuilderResponse object
        /// </summary>
        public static BuilderResponse? ParseBuilderResponse(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<BuilderResponse>(json, options);
            }
            catch (JsonException ex)
            {
                TestOutput.WriteLine($"Failed to parse JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse an HTTP response into a BuilderResponse object
        /// </summary>
        public static async Task<BuilderResponse?> ParseBuilderResponseFromHttpAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseBuilderResponse(json);
        }
    }
}

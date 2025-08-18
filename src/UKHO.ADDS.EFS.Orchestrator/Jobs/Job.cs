using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Jobs;

public partial class Job
{
    public Job()
    {
        JobState = JobState.Created;
        BuildState = BuildState.None;
    }

    public void DoIt()
    {
        JobState = JobState.Completed;
        BuildState = BuildState.Succeeded;
    }

    /// <summary>
    ///     The job id.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     The timestamp of the job creation.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    ///  The last known timestamp for the data standard associated with the job as reported by Sales Catalogue Service,
    /// or DateTime.MinValue if not available.
    /// </summary>
    public DateTime? DataStandardTimestamp { get; set; }

    /// <summary>
    ///     The job data standard, which indicates the format of the data being processed.
    /// </summary>
    public required DataStandard DataStandard { get; init; }

    /// <summary>
    ///     The current state of the job.
    /// </summary>
    [JsonInclude]
    public JobState JobState { get; private set; }

    /// <summary>
    ///     The current build state.
    /// </summary>
    [JsonInclude]
    public BuildState BuildState { get; private set; }

    /// <summary>
    ///     Gets the requested products for the job.
    /// </summary>
    public required ProductNameList RequestedProducts { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the job has any requested products.
    /// </summary>
    public bool HasProducts => RequestedProducts.HasProducts;

    /// <summary>
    ///     Gets the requested filter for the job.
    /// </summary>
    public required string RequestedFilter { get; init; }

    /// <summary>
    ///     The FSS Batch ID associated with the job.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    ///     Gets the correlation ID for the job.
    /// </summary>
    /// <remarks>This is always the Job ID.</remarks>
    /// <returns></returns>
    public string GetCorrelationId() => Id;
}

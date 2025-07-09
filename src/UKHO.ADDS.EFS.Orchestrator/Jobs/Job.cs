using System.Text.Json.Serialization;
using Stateless;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    public class Job : IJsonOnDeserialized
    {
        [JsonIgnore]
        private StateMachine<CombinedState, JobTrigger> _stateMachine;

// Non-nullable field must contain a non-null value when exiting constructor        
#pragma warning disable CS8618 
        public Job()
        {
            // This constructor is only for deserialization and should not be used directly
        }
#pragma warning restore CS8618 

        public Job(string id, DateTime timestamp, DataStandard dataStandard)
        {
            Id = id;
            Timestamp = timestamp;
            DataStandard = dataStandard;

            JobState = JobState.Created;
            BuildState = BuildState.NotScheduled;

            InitializeStateMachine();
        }

        /// <summary>
        ///     The job id.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        ///     The timestamp of the job creation.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        ///     The job data standard, which indicates the format of the data being processed.
        /// </summary>
        public DataStandard DataStandard { get; init; }

        /// <summary>
        /// The current state of the job.
        /// </summary>
        [JsonInclude]
        public JobState JobState { get; private set; }

        /// <summary>
        /// The current build state.
        /// </summary>
        [JsonInclude]
        public BuildState BuildState { get; private set; }

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

        public void MarkUpToDate() => Fire(JobTrigger.MarkUpToDate);

        public void MarkDuplicate() => Fire(JobTrigger.MarkDuplicate);

        public void ScheduleBuild() => Fire(JobTrigger.ScheduleBuild);

        public void AssemblyFailed() => Fire(JobTrigger.AssemblyFailed);

        public void BuildFailed() => Fire(JobTrigger.BuildFailed);

        public void Complete() => Fire(JobTrigger.Completed);

        public void CompleteWithError() => Fire(JobTrigger.CompletedWithError);

        private void Fire(JobTrigger trigger) => _stateMachine.Fire(trigger);

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<CombinedState, JobTrigger>(
                () => new CombinedState(JobState, BuildState),
                x =>
                {
                    JobState = x.JobState;
                    BuildState = x.BuildState;
                });

            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
            // Assembly pipeline
            _stateMachine.Configure(new CombinedState(JobState.Created, BuildState.NotScheduled))
                .Permit(JobTrigger.MarkUpToDate, new CombinedState(JobState.UpToDate, BuildState.NotScheduled))
                .Permit(JobTrigger.MarkDuplicate, new CombinedState(JobState.Duplicate, BuildState.NotScheduled))
                .Permit(JobTrigger.ScheduleBuild, new CombinedState(JobState.Submitted, BuildState.Scheduled))
                .Permit(JobTrigger.AssemblyFailed, new CombinedState(JobState.Failed, BuildState.NotScheduled));

            // Completion pipeline
            _stateMachine.Configure(new CombinedState(JobState.Submitted, BuildState.Scheduled))
                .Permit(JobTrigger.BuildFailed, new CombinedState(JobState.Failed, BuildState.Failed))
                .Permit(JobTrigger.Completed, new CombinedState(JobState.Completed, BuildState.Succeeded))
                .Permit(JobTrigger.CompletedWithError, new CombinedState(JobState.CompletedWithError, BuildState.Succeeded));
        }

        void IJsonOnDeserialized.OnDeserialized()
        {
            InitializeStateMachine();
        }

        private record CombinedState(JobState JobState, BuildState BuildState);
    }
}

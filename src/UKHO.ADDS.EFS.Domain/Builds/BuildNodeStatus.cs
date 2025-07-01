using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildNodeStatus
    {
        public string JobId { get; set; }

        public string Sequence { get; set; }

        public string NodeId { get; set; }

        public NodeResultStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        public double ElapsedMilliseconds { get; set; }
    }
}

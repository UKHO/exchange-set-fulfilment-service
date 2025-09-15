using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    public class ChildNodeResultLogView
    {
        public string Id { get; set; }
        public NodeResultStatus Status { get; set; }
        public Exception Exception { get; set; }
    }
}

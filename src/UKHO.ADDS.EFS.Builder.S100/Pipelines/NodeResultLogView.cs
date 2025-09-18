using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    public class NodeResultLogView
    {
        public BatchId BatchId { get; set; }
        public IList<BuildNodeStatus> BuildNodeStatuses { get; set; } = [];
        public NodeResultStatus Status { get;  set; }
        public Exception Exception { get;  set; }
        public IList<ChildNodeResultLogView> ChildResults { get; set; } = [];
        
    }    
}

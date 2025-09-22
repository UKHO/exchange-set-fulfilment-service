using UKHO.ADDS.EFS.Domain.External;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Generators
{
    public interface ICorrelationIdGenerator
    {
        CorrelationId CreateForJob();
        CorrelationId CreateForScheduler();
    }
}

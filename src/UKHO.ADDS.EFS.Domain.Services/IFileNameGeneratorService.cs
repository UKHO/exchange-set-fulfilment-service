using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services
{
    public interface IFileNameGeneratorService
    {
        string GenerateFileName(string template, JobId jobId, DateTime? date = null);
    }
}

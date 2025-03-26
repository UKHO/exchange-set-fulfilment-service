using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task<Result> Ping();
    }
}

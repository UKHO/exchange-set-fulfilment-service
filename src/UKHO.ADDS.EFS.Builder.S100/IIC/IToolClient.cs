using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task<IResult<bool>> PingAsync();
        Task<IResult<OperationResponse>> AddExchangeSetAsync(JobId jobId, string workspaceName, string authKey);
        Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, JobId jobId, string workspaceName, string authKey);
        Task<IResult<SigningResponse>> SignExchangeSetAsync(JobId jobId, string workspaceName, string authKey);
        Task<IResult<Stream>> ExtractExchangeSetAsync(JobId jobId, string workspaceName, string authKey, string destination);
        Task<IResult<string>> ListWorkspaceAsync(string authKey);
    }
}

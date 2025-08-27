using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Implementation;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task<IResult<bool>> PingAsync();
        Task<IResult<OperationResponse>> AddExchangeSetAsync(JobId exchangeSetId, string authKey, CorrelationId correlationId);
        Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, JobId exchangeSetId, string authKey, CorrelationId correlationId);
        Task<IResult<SigningResponse>> SignExchangeSetAsync(JobId exchangeSetId, string authKey, CorrelationId correlationId);
        Task<IResult<Stream>> ExtractExchangeSetAsync(JobId exchangeSetId, string authKey, CorrelationId correlationId, string destination);
        Task<IResult<string>> ListWorkspaceAsync(string authKey);
    }
}

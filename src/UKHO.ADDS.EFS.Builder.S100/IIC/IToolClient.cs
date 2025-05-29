using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task<IResult<bool>> PingAsync();
        Task<IResult<OperationResponse>> AddExchangeSetAsync(string exchangeSetId, string authKey, string correlationId);
        Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, string exchangeSetId, string authKey, string correlationId);
        Task<IResult<SigningResponse>> SignExchangeSetAsync(string exchangeSetId, string authKey, string correlationId);
        Task<IResult<Stream>> ExtractExchangeSetAsync(string exchangeSetId, string authKey, string correlationId,string destination);
        Task<IResult<string>> ListWorkspaceAsync(string authKey);
    }
}

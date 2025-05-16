using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task PingAsync();
        Task<IResult<OperationResponse>> AddExchangeSetAsync(string exchangeSetId, string authKey, string correlationId);
        Task<IResult<OperationResponse>> AddContentAsync(string workspaceRootPath, string resourceLocation, string exchangeSetId, string authKey, string correlationId);

        Task<IResult<OperationResponse>> AddContentAsync(string workspaceRootPath, string exchangeSetId, string authKey, string correlationId);
        Task<IResult<SigningResponse>> SignExchangeSetAsync(string workspaceRootPath, string exchangeSetId, string authKey, string correlationId);
        Task<IResult<Stream>> ExtractExchangeSetAsync(string workspaceRootPath, string exchangeSetId, string authKey, string correlationId);
        Task<IResult<string>> ListWorkspaceAsync();
    }
}

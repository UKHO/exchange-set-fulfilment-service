namespace ESSFulfilmentService.Common.Messages
{
    public enum ExchangeSetBuilderResult
    {
        Succeeded = 0,

        StartupPipelineFailed = -1,
        AssemblyPipelineFailed = -2,
        DistributionPipelineFailed = -3,
        ProcessingPipelineFailed = -4,
    }
}

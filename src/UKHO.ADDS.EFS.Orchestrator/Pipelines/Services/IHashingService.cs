namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services
{
    internal interface IHashingService
    {
        string CalculateHash(string value);
    }
}

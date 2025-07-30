namespace UKHO.ADDS.Configuration.Client
{
    public interface IExternalServiceRegistry
    {
        Task<Uri?> GetExternalServiceEndpointAsync(string serviceName, bool useDockerHost = false);
    }
}

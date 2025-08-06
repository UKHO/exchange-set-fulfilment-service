namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public interface IExternalServiceRegistry
    {
        Task<IExternalEndpoint> GetServiceEndpointAsync(string serviceName, string tag = "", EndpointHostSubstitution host = EndpointHostSubstitution.None);
    }
}

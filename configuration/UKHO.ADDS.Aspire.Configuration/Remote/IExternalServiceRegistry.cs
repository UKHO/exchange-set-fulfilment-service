namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public interface IExternalServiceRegistry
    {
        IExternalEndpoint GetServiceEndpoint(string serviceName, string tag = "", EndpointHostSubstitution host = EndpointHostSubstitution.None);
    }
}

namespace UKHO.ADDS.Configuration.ExternalServices
{
    public interface IExternalServiceRegistry
    {
        Task<Uri> GetExternalService(string serviceName);
    }
}

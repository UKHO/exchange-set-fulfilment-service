namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public interface IExternalEndpoint
    {
        string Tag { get; }

        EndpointHostSubstitution Host { get; }

        Uri Uri { get; }

        string GetDefaultScope();
    }
}

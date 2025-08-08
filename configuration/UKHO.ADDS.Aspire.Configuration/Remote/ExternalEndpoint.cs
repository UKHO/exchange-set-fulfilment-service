namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public class ExternalEndpoint : IExternalEndpoint
    {
        public string Tag { get; init; }
        public EndpointHostSubstitution Host { get; init; }
        public Uri Uri { get; init; }
    }
}

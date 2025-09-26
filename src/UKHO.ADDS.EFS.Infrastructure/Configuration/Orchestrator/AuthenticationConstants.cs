namespace UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator
{
    public static class AuthenticationConstants
    {
        public const string EfsRole = "ExchangeSetFulfilmentServiceUser";

        public const string MicrosoftLoginUrl = $"https://login.microsoftonline.com/";

        public const string AzureAdScheme = "AzureAd";

        public const string AzureB2CScheme = "AzureB2C";
        
        public const string AdOrB2C = "AdOrB2C";

        public const string OriginHeaderKey = "origin";

        public const string EfsService = "ExchangeSetFulfilmentService";
    }
}

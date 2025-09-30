namespace UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator
{
    public static class GlobalEnvironmentVariables
    {
        public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

        public const string EfsClientId = "AZURE_CLIENT_ID";

        public const string EfsAppRegClientId = "EFS_APP_REG_CLIENTID";

        public const string EfsAppRegTenantId = "EFS_APP_REG_TENANTID";

        public const string EfsB2CAppClientId = "EFS_B2C_APP_CLIENTID";

        public const string EfsB2CAppDomain = "EFS_B2C_APP_DOMAIN";

        public const string EfsB2CAppInstance = "EFS_B2C_APP_INSTANCE";

        public const string EfsB2CAppSignUpSignInPolicy = "EFS_B2C_APP_SIGNIN_POLICY";

        public const string EfsB2CAppTenantId = "EFS_B2C_APP_TENANTID";
    }
}

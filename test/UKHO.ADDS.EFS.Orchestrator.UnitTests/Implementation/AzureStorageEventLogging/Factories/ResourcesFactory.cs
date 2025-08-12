namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging.Factories
{
    /// <summary>
    ///     The resources factory model
    /// </summary>
    public class ResourcesFactory
    {
        /// <summary>
        ///     The default constructor
        /// </summary>
        public ResourcesFactory()
        {
            SuccessTemplateMessage = "Azure Storage Logging: A blob with the error details was created at {{BlobFullName}}. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {{ReasonPhrase}} ResponseCode: {{StatusCode}} RequestId: {{RequestId}} Sha256: {{FileSHA}} FileSize(Bs): {{FileSize}} FileModifiedDate: {{ModifiedDate}}";
            FailureTemplateMessage = "Azure Storage Logging: Storing blob failed. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {{ReasonPhrase}} ResponseCode: {{StatusCode}} RequestId: {{RequestId}}";
        }

        public string SuccessTemplateMessage { get; }
        public string FailureTemplateMessage { get; }

    }
}

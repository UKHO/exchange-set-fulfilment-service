namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    public class ExtractExchangeSetLogView
    {
        public string ExchangeSetId { get; set; }
        public string ExchangeSetName { get; set; }
        //public string FilePath { get; set; }
        public string CorrelationId { get; set; }
        public string Error { get; set; }
    }
}

namespace UKHO.ADDS.EFS.Domain.Products
{
    public class DataStandardTimestamp
    {
        public DataStandard DataStandard { get; set; } = DataStandard.S100;

        public DateTime? Timestamp { get; set; }
    }
}

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    public class DataStandardTimestamp
    {
        public DataStandard DataStandard { get; set; } = DataStandard.S100;

        public DateTime? Timestamp { get; set; }
    }
}

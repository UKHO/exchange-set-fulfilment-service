namespace UKHO.ADDS.EFS.Configuration.Builder
{
    public class FileShareServiceSettings
    {
        public string ProductName { get; set; }
        public string EditionNumber { get; set; }
        public string UpdateNumber { get; set; }
        public string BusinessUnit { get; set; }
        public string ProductType { get; set; }
        public int ParallelSearchTaskCount { get; set; }
        public int UpdateNumberLimit { get; set; }
        public int ProductLimit { get; set; }
        public int Limit { get; set; }
        public int Start { get; set; }
    }
}

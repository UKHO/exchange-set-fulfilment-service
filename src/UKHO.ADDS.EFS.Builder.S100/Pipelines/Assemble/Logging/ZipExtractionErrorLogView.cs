namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class ZipExtractionErrorLogView
    {
        public string ZipFilePath { get; set; }
        
        public string DestinationDirectoryPath { get; set; }
        
        public string ExceptionMessage { get; set; }
        
        public string ExceptionType { get; set; }
        
        public string FileName { get; set; }
    }
}

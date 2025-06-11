namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    internal class ExceptionLogView
    {
        public string ExceptionType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string StackTrace { get; set; } = string.Empty;        
    }
}

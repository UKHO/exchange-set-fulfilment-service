namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    /// <summary>
    /// Represents the state of ZIP file extraction process, tracking resource consumption
    /// to prevent zip bomb attacks and enforce security limits.
    /// </summary>
    internal sealed class ZipExtractionState
    {
        /// <summary>
        /// Gets or sets the total size of all extracted files in bytes.
        /// Used to enforce maximum total extraction size limits.
        /// </summary>
        public long TotalExtractedSize { get; set; }
    }
}

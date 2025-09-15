namespace UKHO.ADDS.EFS.Domain.Builds
{
    public class BuildFileDetail
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public required string FileName { get; init; }
        /// <summary>
        /// Gets or sets the file hash value.
        /// </summary>
        public required string Hash { get; init; }
    }
}

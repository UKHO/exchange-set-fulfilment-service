namespace UKHO.ADDS.EFS.Domain.Builds
{
    public class BuildCommitInfo
    {
        private List<BuildFileDetail> _fileDetails;

        public BuildCommitInfo()
        {
            _fileDetails = [];
        }

        /// <summary>
        /// Gets or sets the collection of file details.
        /// </summary>
        public IEnumerable<BuildFileDetail> FileDetails
        {
            get => _fileDetails;
            set => _fileDetails = value?.ToList() ?? [];
        }

        /// <summary>
        /// Adds a file detail to the collection.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="hash">The file hash.</param>
        public void AddFileDetail(string fileName, string hash)
        {
            _fileDetails.Add(new BuildFileDetail
            {
                FileName = fileName,
                Hash = hash
            });
        }
    }
}

using System.IO;

namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
{
    public class FileSystemWrapper : IFileSystemWrapper
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}

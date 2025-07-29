namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
{
    public interface IFileSystemWrapper
    {
        bool DirectoryExists(string path);
    }
}

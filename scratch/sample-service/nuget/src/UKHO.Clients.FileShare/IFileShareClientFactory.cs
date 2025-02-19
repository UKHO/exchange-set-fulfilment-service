namespace UKHO.Clients.FileShare
{
    public interface IFileShareClientFactory
    {
        Task<IFileShareClient> CreateFileShareClientAsync();
    }
}

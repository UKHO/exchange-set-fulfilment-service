namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public interface IBatchHandle
    {
        string BatchId { get; }
        List<FileDetail> FileDetails { get; }
    }

    public class BatchHandle : IBatchHandle
    {
        public string BatchId { get; }

        public List<FileDetail> FileDetails { get; }

        public BatchHandle(string batchId)
        {
            BatchId = batchId;
            FileDetails = new List<FileDetail>();
        }

        public void AddFile(string filename, string hash)
        {
            FileDetails.Add(new FileDetail { FileName = filename, Hash = hash });
        }
    }
}

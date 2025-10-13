namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models
{
    /// <summary>
    /// Static class to store file blocks in memory, shared between upload and write endpoints
    /// </summary>
    internal static class FileBlockStorage
    {
        // Dictionary structure: { "batchId:fileName": { "blockId": blockData } }
        internal static readonly Dictionary<string, Dictionary<string, byte[]>> FileBlocks = new();
    }
}

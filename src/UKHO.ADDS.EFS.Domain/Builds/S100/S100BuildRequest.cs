namespace UKHO.ADDS.EFS.Domain.Builds.S100
{
    public class S100BuildRequest : BuildRequest
    {
        /// <summary>
        /// The IIC workspace key
        /// </summary>
        public required string WorkspaceKey { get; init; }
    }
}

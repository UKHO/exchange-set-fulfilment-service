namespace UKHO.ADDS.EFS.Domain.Services.Storage
{
    /// <summary>
    /// Portable queue message representation independent of Azure SDK.
    /// </summary>
    public readonly record struct QueueMessage(string MessageId, string PopReceipt, string MessageText);
}

namespace UKHO.ADDS.EFS.Domain.Services.Storage
{
    /// <summary>
    ///     Abstraction of a queue that can be backed by Azure Storage queues or any other implementation.
    ///     Does not expose Azure SDK types.
    /// </summary>
    public interface IQueue
    {
        // Management
        Task CreateIfNotExistsAsync(CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);

        // Send
        Task EnqueueAsync(string messageText, CancellationToken cancellationToken = default);

        // Receive/Peek/Delete
        Task<IReadOnlyList<QueueMessage>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken = default);
        Task<QueueMessage?> ReceiveOneAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> PeekMessageTextsAsync(int maxMessages, CancellationToken cancellationToken = default);
        Task DeleteAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default);
    }
}

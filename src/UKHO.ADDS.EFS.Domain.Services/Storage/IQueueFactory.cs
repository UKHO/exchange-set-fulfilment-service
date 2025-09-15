namespace UKHO.ADDS.EFS.Domain.Services.Storage
{
    /// <summary>
    /// Factory for creating IQueue instances and performing queue account-level operations
    /// without exposing Azure types.
    /// </summary>
    public interface IQueueFactory
    {
        // Create a queue client by name
        IQueue GetQueue(string queueName);
    }
}

using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Domain.Services.Storage;

namespace UKHO.ADDS.EFS.Infrastructure.Storage.Queues
{
    internal sealed class AzureQueue : IQueue
    {
        private readonly QueueClient _client;

        public AzureQueue(QueueClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
            => _client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => _client.ClearMessagesAsync(cancellationToken: cancellationToken);

        public Task EnqueueAsync(string messageText, CancellationToken cancellationToken = default)
            => _client.SendMessageAsync(messageText, cancellationToken);

        public async Task<IReadOnlyList<QueueMessage>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken = default)
        {
            var result = await _client.ReceiveMessagesAsync(maxMessages, cancellationToken: cancellationToken);
            return result.Value.Select(m => new QueueMessage(m.MessageId, m.PopReceipt, m.MessageText)).ToList();
        }

        public async Task<QueueMessage?> ReceiveOneAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (result.Value is null)
            {
                return null;
            }

            var m = result.Value;
            return new QueueMessage(m.MessageId, m.PopReceipt, m.MessageText);
        }

        public async Task<IReadOnlyList<string>> PeekMessageTextsAsync(int maxMessages, CancellationToken cancellationToken = default)
        {
            var result = await _client.PeekMessagesAsync(maxMessages: maxMessages, cancellationToken: cancellationToken);

            return result.Value.Select(m => m.MessageText).ToList();
        }

        public Task DeleteAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default)
            => _client.DeleteMessageAsync(messageId, popReceipt, cancellationToken);
    }
}

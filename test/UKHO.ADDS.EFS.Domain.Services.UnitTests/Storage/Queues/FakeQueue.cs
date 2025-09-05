using System.Collections.Concurrent;
using UKHO.ADDS.EFS.Domain.Services.Storage;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Queues
{
    internal sealed class FakeQueue : IQueue
    {
        private readonly ConcurrentDictionary<string, Item> _byId = new();

        private readonly ConcurrentQueue<Item> _queue = new();

        public Task CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            while (_queue.TryDequeue(out var _))
            {
            }

            _byId.Clear();
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string messageText, CancellationToken cancellationToken = default)
        {
            if (messageText is null)
            {
                throw new ArgumentNullException(nameof(messageText));
            }

            var id = Guid.NewGuid().ToString("N");
            var item = new Item
            {
                Id = id, Text = messageText
            };
            _queue.Enqueue(item);
            _byId[id] = item;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<QueueMessage>> ReceiveAsync(int maxMessages, CancellationToken cancellationToken = default)
        {
            if (maxMessages < 1 || maxMessages > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMessages), "maxMessages must be between 1 and 32.");
            }

            var results = new List<QueueMessage>();
            // Enumerate without removing; preserve FIFO
            var snapshot = _queue.ToArray();
            for (var i = 0; i < Math.Min(maxMessages, snapshot.Length); i++)
            {
                var it = snapshot[i];
                var pop = Guid.NewGuid().ToString("N");
                it.LastPopReceipt = pop;
                results.Add(new QueueMessage(it.Id, pop, it.Text));
            }

            return Task.FromResult<IReadOnlyList<QueueMessage>>(results);
        }

        public async Task<QueueMessage?> ReceiveOneAsync(CancellationToken cancellationToken = default)
        {
            var list = await ReceiveAsync(1, cancellationToken);
            return list.Count == 0 ? null : list[0];
        }

        public Task<IReadOnlyList<string>> PeekMessageTextsAsync(int maxMessages, CancellationToken cancellationToken = default)
        {
            if (maxMessages < 1 || maxMessages > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMessages), "maxMessages must be between 1 and 32.");
            }

            var snapshot = _queue.ToArray();
            var texts = snapshot.Take(Math.Min(maxMessages, snapshot.Length)).Select(i => i.Text).ToList();
            return Task.FromResult<IReadOnlyList<string>>(texts);
        }

        public Task DeleteAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default)
        {
            if (messageId is null)
            {
                throw new ArgumentNullException(nameof(messageId));
            }

            if (popReceipt is null)
            {
                throw new ArgumentNullException(nameof(popReceipt));
            }

            if (!_byId.TryGetValue(messageId, out var item) || !string.Equals(item.LastPopReceipt, popReceipt, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Message not found or pop receipt mismatch.");
            }

            // Remove from dictionary
            _byId.TryRemove(messageId, out var _);

            // Rebuild queue without the item (rare in tests, acceptable cost)
            var remaining = new Queue<Item>();
            while (_queue.TryDequeue(out var it))
            {
                if (!ReferenceEquals(it, item))
                {
                    remaining.Enqueue(it);
                }
            }

            foreach (var it in remaining)
            {
                _queue.Enqueue(it);
            }

            return Task.CompletedTask;
        }

        private sealed class Item
        {
            public required string Id { get; init; }
            public string? LastPopReceipt { get; set; }
            public required string Text { get; init; }
        }
    }
}

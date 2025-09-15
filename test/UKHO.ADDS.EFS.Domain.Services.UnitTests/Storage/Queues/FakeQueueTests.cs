using UKHO.ADDS.EFS.Domain.Services.Storage;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Queues
{
    public class FakeQueueTests
    {
        [Fact]
        public async Task Create_Clear_Enqueue_Receive_Delete_Peek_Flow_Works()
        {
            IQueue q = new FakeQueue();
            await q.CreateIfNotExistsAsync();

            await q.EnqueueAsync("m1");
            await q.EnqueueAsync("m2");

            var peek = await q.PeekMessageTextsAsync(2);
            Assert.Equal(["m1", "m2"], peek);

            var received = await q.ReceiveAsync(2);
            Assert.Equal(2, received.Count);
            Assert.Equal("m1", received[0].MessageText);
            Assert.NotEmpty(received[0].MessageId);
            Assert.NotEmpty(received[0].PopReceipt);

            await q.DeleteAsync(received[0].MessageId, received[0].PopReceipt);

            var one = await q.ReceiveOneAsync();
            Assert.NotNull(one);
            Assert.Equal("m2", one!.Value.MessageText);

            await q.DeleteAsync(one.Value.MessageId, one.Value.PopReceipt);

            var empty = await q.ReceiveOneAsync();
            Assert.Null(empty);

            await q.ClearAsync();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(33)]
        public async Task Receive_InvalidMax_Throws(int max)
        {
            IQueue q = new FakeQueue();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.ReceiveAsync(max));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(33)]
        public async Task Peek_InvalidMax_Throws(int max)
        {
            IQueue q = new FakeQueue();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.PeekMessageTextsAsync(max));
        }

        [Fact]
        public async Task Enqueue_Null_Throws()
        {
            IQueue q = new FakeQueue();
            await Assert.ThrowsAsync<ArgumentNullException>(() => q.EnqueueAsync(null!));
        }

        [Fact]
        public async Task Delete_InvalidIdOrPopReceipt_Throws()
        {
            IQueue q = new FakeQueue();
            await q.EnqueueAsync("m");
            var msg = await q.ReceiveOneAsync();
            Assert.NotNull(msg);

            await Assert.ThrowsAsync<ArgumentNullException>(() => q.DeleteAsync(null!, msg!.Value.PopReceipt));
            await Assert.ThrowsAsync<ArgumentNullException>(() => q.DeleteAsync(msg!.Value.MessageId, null!));
            await Assert.ThrowsAsync<InvalidOperationException>(() => q.DeleteAsync(msg!.Value.MessageId, "wrong"));
        }
    }
}

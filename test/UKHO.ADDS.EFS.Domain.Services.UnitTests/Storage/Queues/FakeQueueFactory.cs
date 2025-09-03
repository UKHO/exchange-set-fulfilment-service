using System.Text.RegularExpressions;
using UKHO.ADDS.EFS.Domain.Services.Storage;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Queues
{
    internal sealed class FakeQueueFactory : IQueueFactory
    {
        // Azure Storage queue name rules (simplified):
        // - 3 to 63 chars
        // - lowercase letters, numbers, and dash only
        // - start with letter or number
        // - no consecutive dashes
        // - end with letter or number
        private static readonly Regex NameRegex = new("^(?=.{3,63}$)(?!-)(?!.*--)[a-z0-9-]+(?<!-)$", RegexOptions.Compiled);

        public IQueue GetQueue(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Queue name must be provided.", nameof(queueName));
            }

            if (!NameRegex.IsMatch(queueName))
            {
                throw new ArgumentException("Queue name does not conform to Azure Storage naming rules.", nameof(queueName));
            }

            return new FakeQueue();
        }
    }
}

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation.Entities
{
    public class CompoundKeyTestEntity
    {
        public required string PartitionKey { get; init; }

        public required string RowKey { get; init; }
    }
}

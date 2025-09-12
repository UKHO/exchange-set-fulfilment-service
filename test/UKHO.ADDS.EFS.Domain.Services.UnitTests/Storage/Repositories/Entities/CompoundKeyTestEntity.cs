namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Repositories.Entities
{
    public class CompoundKeyTestEntity
    {
        public required string PartitionKey { get; init; }

        public required string RowKey { get; init; }
    }
}

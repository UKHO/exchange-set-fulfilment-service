namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation
{
    internal static class JsonEntityBuilder
    {
        private const int MaxPropertiesCount = 14;
        private const int MaxChunkSize = 65536; // Each property can hold up to 64 KB

        public static JsonEntity BuildEntity(string serializedData, string partitionKey, string rowKey)
        {
            // Check if the data is too large to fit in 14 properties
            if (serializedData.Length > MaxPropertiesCount * MaxChunkSize)
            {
                throw new InvalidOperationException("The serialized data is too large");
            }

            var internalEntity = new JsonEntity { PartitionKey = partitionKey, RowKey = rowKey };

            var dataLength = serializedData.Length;
            for (var i = 0; i < MaxPropertiesCount; i++)
            {
                // Calculate the starting index for each chunk
                var startIndex = i * MaxChunkSize;

                // If this is the last chunk, take the remaining data
                if (startIndex < dataLength)
                {
                    var chunkSize = Math.Min(MaxChunkSize, dataLength - startIndex);
                    var chunk = serializedData.Substring(startIndex, chunkSize);

                    internalEntity.GetType().GetProperty($"P{i}")?.SetValue(internalEntity, chunk);
                }
            }

            return internalEntity;
        }

        public static string RebuildEntityData(JsonEntity entity) =>
            string.Concat(
                entity.P0,
                entity.P1,
                entity.P2,
                entity.P3,
                entity.P4,
                entity.P5,
                entity.P6,
                entity.P7,
                entity.P8,
                entity.P9,
                entity.P10,
                entity.P11,
                entity.P12,
                entity.P13
            );
    }
}

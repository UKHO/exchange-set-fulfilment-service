namespace UKHO.ADDS.EFS.Domain.Files
{
    public class BatchFile
    {
        public required string Filename { get; init; }

        public required long? FileSize { get; init; }

        public required string MimeType { get; init; }

        public required string Hash { get; init; }

        public required AttributeList Attributes { get; init; }

        public required Link Link { get; init; }
    }
}

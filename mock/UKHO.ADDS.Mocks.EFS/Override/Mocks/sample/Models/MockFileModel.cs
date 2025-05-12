using System.ComponentModel;

namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.sample.Models
{
    public class MockFileModel
    {
        [Description("The file name")] public required string Name { get; set; }

        [Description("The file length in bytes")] public required long Length { get; set; }

        [Description("The MIME type of the file")] public required string MimeType { get; set; }
    }
}

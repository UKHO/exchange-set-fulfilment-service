using System.IO.Compression;
using Aspire.Hosting;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.EndToEndTests.Helper
{
    public class ZipUtility
    {
        public static async Task<string> DownloadExchangeSetAsZipAsync(string jobId, DistributedApplication app)
        {
            var httpClientMock = app.CreateHttpClient(ProcessNames.MockService);
            var mockResponse = await httpClientMock.GetAsync($"/_admin/files/FSS/S100-ExchangeSets/V01X01_{jobId}.zip");
            mockResponse.EnsureSuccessStatusCode();

            await using var zipStream = await mockResponse.Content.ReadAsStreamAsync();
            var destinationFilePath = Path.Combine(TestBase.ProjectDirectory!, "out", $"V01X01_{jobId}.zip");

            // Ensure the directory exists
            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory!);
            }
            await using var fileStream =
            new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await zipStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            return destinationFilePath;
        }

        public static void CompareZipFilesExactMatch(string sourceZipPath, string targetZipPath)
        {
            using var archive1 = ZipFile.OpenRead(sourceZipPath);
            using var archive2 = ZipFile.OpenRead(targetZipPath);

            var entries1 = archive1.Entries.Select(e => e.FullName.Replace('\\', '/')).OrderBy(e => e).ToList();
            var entries2 = archive2.Entries.Select(e => e.FullName.Replace('\\', '/')).OrderBy(e => e).ToList();

            // Compare file names and structure
            Assert.Equal(entries1.Count, entries2.Count);
            for (var i = 0; i < entries1.Count; i++)
            {
                Assert.Equal(entries1[i], entries2[i]);
            }
        }
    }
}

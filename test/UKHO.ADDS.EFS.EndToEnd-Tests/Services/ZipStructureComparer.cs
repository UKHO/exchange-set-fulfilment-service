using System.IO.Compression;
using Aspire.Hosting;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.EndToEndTests.Services
{
    public class ZipStructureComparer
    {
        /// <summary>
        /// Downloads a zip file for the given jobId from the mock service and saves it to the out directory.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="app">The distributed application instance.</param>
        /// <returns>The file path of the downloaded zip.</returns>
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

        /// <summary>
        /// Compares the directory structure of two zip files for an exact match.
        /// </summary>
        /// <param name="sourceZipPath">The path to the source zip file.</param>
        /// <param name="targetZipPath">The path to the target zip file.</param>
        public static void CompareZipFilesExactMatch(string sourceZipPath, string targetZipPath)
        {
            using var archive1 = ZipFile.OpenRead(sourceZipPath);
            using var archive2 = ZipFile.OpenRead(targetZipPath);

            static string? GetDirectory(string fullName)
            {
                var idx = fullName.LastIndexOf('/');
                return idx > 0 ? fullName.Substring(0, idx) : fullName;
            }

            var entries1 = archive1.Entries.Select(e => GetDirectory(e.FullName)).Distinct().OrderBy(e => e).ToList();
            var entries2 = archive2.Entries.Select(e => GetDirectory(e.FullName)).Distinct().OrderBy(e => e).ToList();

            //Compare the entries of both zip files
            Assert.Equal(entries1, entries2);
        }
    }
}

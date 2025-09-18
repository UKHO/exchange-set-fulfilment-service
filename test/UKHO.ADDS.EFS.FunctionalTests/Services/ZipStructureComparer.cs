using System.IO.Compression;
using Aspire.Hosting;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class ZipStructureComparer
    {
        /// <summary>
        /// Downloads a zip file for the given jobId from the mock service and saves it to the out directory.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="app">The distributed application instance.</param>
        /// <returns>The file path of the downloaded zip.</returns>
        public static async Task<string> DownloadExchangeSetAsZipAsync(string jobId)
        {
            var httpClientMock = AspireResourceSingleton.App!.CreateHttpClient(ProcessNames.MockService);
            var mockResponse = await httpClientMock.GetAsync($"/_admin/files/FSS/S100-ExchangeSets/V01X01_{jobId}.zip");
            mockResponse.EnsureSuccessStatusCode();

            await using var zipStream = await mockResponse.Content.ReadAsStreamAsync();
            var destinationFilePath = Path.Combine(AspireResourceSingleton.ProjectDirectory!, "out", $"V01X01_{jobId}.zip");

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
        /// Compares two ZIP files to ensure their directory structures match exactly.
        /// Optionally verifies that specified product files are present in the source ZIP.
        /// </summary>
        /// <param name="sourceZipPath">Path to the source ZIP file.</param>
        /// <param name="targetZipPath">Path to the target ZIP file.</param>
        /// <param name="products">Comma-separated list of expected product file names (optional).</param>
        public static void CompareZipFilesExactMatch(string sourceZipPath, string targetZipPath, string[]? products = null)
        {
            // Open both ZIP archives for reading
            using var sourceArchive = ZipFile.OpenRead(sourceZipPath);
            using var targetArchive = ZipFile.OpenRead(targetZipPath);

            // Helper method to extract the directory path from a full entry name
            static string? GetDirectoryPath(string fullName)
            {
                var idx = fullName.LastIndexOf('/');
                return idx > 0 ? fullName[..idx] : fullName;
            }

            // Get distinct directory paths from source archive
            var sourceDirectories = sourceArchive.Entries
                .Select(e => GetDirectoryPath(e.FullName))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            // Get distinct directory paths from target archive
            var targetDirectories = targetArchive.Entries
                .Select(e => GetDirectoryPath(e.FullName))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            // Compare directory structures of both ZIP files
            Assert.Equal(sourceDirectories, targetDirectories);

            // If product names are specified, validate their presence in the source archive
            if (products != null)
            {
                var expectedProductPaths = new List<string>();
                foreach (var productName in products)
                {
                    var productIdentifier = productName[..3];
                    var folderName = productName[3..7];
                    if (productIdentifier == "101")
                    {
                        expectedProductPaths.Add($"S100_ROOT/S-{productIdentifier}/SUPPORT_FILES/{productName}");
                    }
                    expectedProductPaths.Add($"S100_ROOT/S-{productIdentifier}/DATASET_FILES/{folderName}/{productName}");
                }
                //added file expected other than product name
                expectedProductPaths.Add("S100_ROOT/CATALOG");

                // Extract actual product file names from the target archive
                var actualProductPaths = targetArchive.Entries
                    .Where(e => e.FullName.Contains('.')) // Assuming product files have extensions
                    .Select(e => e.FullName[..e.FullName.IndexOf('.')]) // Get the file name without extension
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
                expectedProductPaths.Sort();
                // Compare expected and actual product file names
                Assert.Equal(expectedProductPaths, actualProductPaths!);
            }
        }

    }
}

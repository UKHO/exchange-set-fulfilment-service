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

        public static void CompareZipFolderStructure(string sourceZipPath, string targetZipPath)
        {
            using var archive1 = ZipFile.OpenRead(sourceZipPath);
            using var archive2 = ZipFile.OpenRead(targetZipPath);

            // Extract folder paths from entries
            var folders1 = archive1.Entries
                .Select(e => e.FullName.Replace('\\', '/'))
                .SelectMany(e => GetParentFolders(e))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            var folders2 = archive2.Entries
                .Select(e => e.FullName.Replace('\\', '/'))
                .SelectMany(e => GetParentFolders(e))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            // Compare folder structures
            Assert.Equal(folders1.Count, folders2.Count);
            for (var i = 0; i < folders1.Count; i++)
            {
                Assert.Equal(folders1[i], folders2[i]);
            }
        }

        // Helper method to get all parent folders from a path
        private static IEnumerable<string> GetParentFolders(string entryPath)
        {
            var folders = new List<string>();
            var parts = entryPath.Split('/');

            var currentPath = "";
            for (var i = 0; i < parts.Length - 1; i++) // Exclude file name part
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}/{parts[i]}";
                folders.Add(currentPath + "/"); // Ensure it ends with '/'
            }
            return folders;
        }
    }
}

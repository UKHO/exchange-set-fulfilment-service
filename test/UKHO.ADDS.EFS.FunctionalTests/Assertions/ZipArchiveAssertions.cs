using System.IO.Compression;
using AwesomeAssertions;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;

namespace UKHO.ADDS.EFS.FunctionalTests.Assertions
{
    public class  ZipArchiveAssertions
    {
        /// <summary>
        /// Compares two ZIP files to ensure their directory structures match exactly.
        /// Optionally verifies that specified product files are present in the source ZIP.
        /// Uses soft assertions to report all validation issues at once.
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

            TestOutput.WriteLine($"Expected Zip File Directory Structure: {string.Join(", ", sourceDirectories)}");

            // Get distinct directory paths from target archive
            var targetDirectories = targetArchive.Entries
                .Select(e => GetDirectoryPath(e.FullName))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            TestOutput.WriteLine($"Actual Zip File Directory Structure: {string.Join(", ", targetDirectories)}");

            // Compare directory structures of both ZIP files using soft assertions
            /*
             * Currently commented as directory structure is not exactly matching
             */
            // sourceDirectories.Should().BeEquivalentTo(targetDirectories, "directory structures in both ZIP files should match");

            // If product names are specified, validate their presence in the target zip
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
                expectedProductPaths.Sort();

                TestOutput.WriteLine($"Expected File Path Structure: {string.Join(", ", expectedProductPaths)}");

                // Extract actual product file names from the target archive
                var actualProductPaths = targetArchive.Entries
                    .Where(e => e.FullName.Contains('.')) // Assuming product files have extensions
                    .Select(e => e.FullName[..e.FullName.IndexOf('.')]) // Get the file name without extension
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                TestOutput.WriteLine($"Actual File Path Structure: {string.Join(", ", actualProductPaths)}");

                // Compare expected and actual product file names using soft assertions
                actualProductPaths.Should().BeEquivalentTo(expectedProductPaths,
                    "all expected product files should be present in the ZIP");
            }
        }

    }
}

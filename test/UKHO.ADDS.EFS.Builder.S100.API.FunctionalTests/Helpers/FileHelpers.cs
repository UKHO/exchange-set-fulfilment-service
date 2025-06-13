using System.IO.Compression;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers
{
    public class FileHelpers
    {
        // Add this method to your FileHelpers class
        public void CompareZipFolderAndFileStructures(string sourceZipPath, string targetZipPath)
        {
            var sourceExtractPath = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid());
            var targetExtractPath = Path.Combine(Path.GetTempPath(), "target_" + Guid.NewGuid());

            try
            {
                // Extract both zips
                ZipFile.ExtractToDirectory(sourceZipPath, sourceExtractPath);
                ZipFile.ExtractToDirectory(targetZipPath, targetExtractPath);

                // Get all relative folder paths (excluding root)
                var sourceFolders = Directory.GetDirectories(sourceExtractPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(sourceExtractPath, f).TrimEnd(Path.DirectorySeparatorChar))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var targetFolders = Directory.GetDirectories(targetExtractPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(targetExtractPath, f).TrimEnd(Path.DirectorySeparatorChar))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Get all relative file paths
                var sourceFiles = Directory.GetFiles(sourceExtractPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(sourceExtractPath, f))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var targetFiles = Directory.GetFiles(targetExtractPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(targetExtractPath, f))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Assert: Folder structure matches
               Assert.That(targetFolders.SetEquals(sourceFolders), Is.True, "Folder structures do not match.");

                // Assert: File names and structure match
                Assert.That(targetFiles.SetEquals(sourceFiles), Is.True, "File structures do not match.");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(sourceExtractPath))
                    Directory.Delete(sourceExtractPath, true);
                if (Directory.Exists(targetExtractPath))
                    Directory.Delete(targetExtractPath, true);
            }
        }
    }
}

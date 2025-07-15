using System.IO.Compression;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers
{
    public class FileHelpers
    {        
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

                // Find non-matching folders
                var foldersOnlyInSource = sourceFolders.Except(targetFolders).ToList();
                var foldersOnlyInTarget = targetFolders.Except(sourceFolders).ToList();

                // Find non-matching files
                var filesOnlyInSource = sourceFiles.Except(targetFiles).ToList();
                var filesOnlyInTarget = targetFiles.Except(sourceFiles).ToList();

                // Assert: Folder structure matches, with details
                Assert.That(foldersOnlyInSource.Count == 0 && foldersOnlyInTarget.Count == 0,
                    $"Folder structures do not match.\n" +
                    (foldersOnlyInSource.Count > 0 ? $"Folders only in source: {string.Join(", ", foldersOnlyInSource)}\n" : "") +
                    (foldersOnlyInTarget.Count > 0 ? $"Folders only in target: {string.Join(", ", foldersOnlyInTarget)}\n" : ""));

                // Assert: File names and structure match, with details
                Assert.That(filesOnlyInSource.Count == 0 && filesOnlyInTarget.Count == 0,
                    $"File structures do not match.\n" +
                    (filesOnlyInSource.Count > 0 ? $"Files only in source: {string.Join(", ", filesOnlyInSource)}\n" : "") +
                    (filesOnlyInTarget.Count > 0 ? $"Files only in target: {string.Join(", ", filesOnlyInTarget)}\n" : ""));
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

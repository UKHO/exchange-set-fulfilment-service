using System.IO.Compression;

namespace UKHO.ADDS.EFS.S100.API.FunctionalTests.Helpers
{
    public class FileHelpers
    {    

        public (HashSet<string> Folders, HashSet<string> Files) GetZipStructure(string zipPath)
        {
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                // Normalize path separators
                var entryPath = entry.FullName.Replace('\\', '/').TrimEnd('/');

                if (string.IsNullOrEmpty(entryPath))
                    continue;

                if (entry.FullName.EndsWith("/"))
                {
                    // It's a directory entry
                    folders.Add(entryPath);
                }
                else
                {
                    // It's a file entry
                    files.Add(entryPath);

                    // Add all parent folders
                    var lastSlash = entryPath.LastIndexOf('/');
                    while (lastSlash > 0)
                    {
                        var folder = entryPath.Substring(0, lastSlash);
                        folders.Add(folder);
                        lastSlash = folder.LastIndexOf('/');
                    }
                }
            }
            return (folders, files);
        }

        public void CompareZipFolderAndFileStructures(string sourceZipPath, string targetZipPath)
        {
            var (sourceFolders, sourceFiles) = GetZipStructure(sourceZipPath);
            var (targetFolders, targetFiles) = GetZipStructure(targetZipPath);

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
    }
}

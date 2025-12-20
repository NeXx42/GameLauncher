using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace GameLibrary.Logic
{
    public static class FileManager
    {

        //public static void SaveScreenshot(obj bmp)
        //{
        //    Cleanup();
        //    bmp.Save(Path.Combine(GetTempLocation(), "temp.jpg"), ImageFormat.Jpeg);
        //}

        public static async Task StartDeletion(GameDto game)
        {

        }

        public static async Task UpdateGameIcon(int gameId, Uri newIconPath)
        {
            GameDto? game = LibraryHandler.GetGameFromId(gameId);

            if (!File.Exists(newIconPath.LocalPath) || game == null)
            {
                return;
            }

            string localPath = $"{Guid.NewGuid()}.png";
            File.Copy(newIconPath.LocalPath, Path.Combine(game.getAbsoluteFolderLocation, localPath));

            await game.UpdateGameIcon(localPath);
        }

        public static Task MoveGameToItsLibrary(dbo_Game game, string existingFolderLocation, string libraryRootLocation)
        {
            Directory.Move(existingFolderLocation, Path.Combine(libraryRootLocation, game.gameFolder));
            return Task.CompletedTask;
        }


        public static async Task<List<FolderEntry>> CrawlGames(string[] paths)
        {
            List<FolderEntry> foundEntries = new List<FolderEntry>();

            List<string> topLevelFolders = new List<string>();
            List<string> extracts = new List<string>();

            foreach (string path in paths)
            {
                topLevelFolders.AddRange(Directory.GetDirectories(path));
                extracts.AddRange(Directory.GetFiles(path).Where(IsZip));
            }

            foreach (string extract in extracts)
            {
                string archiveDir = Path.Combine(Path.GetDirectoryName(extract)!, Path.GetFileNameWithoutExtension(extract));
                int extractedFolder = topLevelFolders.IndexOf(archiveDir);
                FolderEntry folder = new FolderEntry(extract, extractedFolder >= 0 ? topLevelFolders[extractedFolder] : null);

                if (extractedFolder >= 0)
                    topLevelFolders.RemoveAt(extractedFolder);

                foundEntries.Add(folder);
            }

            foreach (string remainingFolder in topLevelFolders)
            {
                FolderEntry folder = new FolderEntry(null, remainingFolder);
                foundEntries.Add(folder);
            }

            return foundEntries;
        }

        public static bool IsExecutable(string path)
        {
            return path.EndsWith(".exe");
        }


        public static bool IsZip(string path)
        {
            return path.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".rar", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);
        }

        public static async Task<string?> ExtractFolder(string archiveFile)
        {
            if (!File.Exists(archiveFile))
                return string.Empty;

            bool requiresPassword = false;
            bool didExtract = false;

            using (IArchive? passwordCheckArchive = ArchiveFactory.Open(archiveFile))
            {
                requiresPassword = passwordCheckArchive.Entries.Any(x => x.IsEncrypted);
            }

            string extractPath = Path.Combine(Path.GetDirectoryName(archiveFile)!, Path.GetFileNameWithoutExtension(archiveFile));
            Directory.CreateDirectory(extractPath);

            ExtractionOptions extractionOptions = new ExtractionOptions()
            {
                ExtractFullPath = true,
                Overwrite = true,
            };

            if (requiresPassword)
            {
                string? password = await DependencyManager.uiLinker!.OpenStringInputModal("Archive Password") ?? string.Empty;

                using (IArchive? archive = ArchiveFactory.Open(archiveFile, new SharpCompress.Readers.ReaderOptions() { Password = password }))
                {
                    await DependencyManager.uiLinker!.OpenLoadingModal(false, async () => archive.WriteToDirectory(extractPath, extractionOptions));
                    didExtract = true;
                }
            }
            else
            {
                using (IArchive? archive = ArchiveFactory.Open(archiveFile))
                {
                    await Extract(archive, extractPath, extractionOptions);
                    didExtract = true;
                }
            }

            if (didExtract)
            {
                return extractPath;
            }

            return string.Empty;

            async Task Extract(IArchive archive, string outputPath, ExtractionOptions options)
            {
                Func<Task>[] tasks = archive.Entries
                    .Where(e => !e.IsDirectory)
                    .Select(entry => (Func<Task>)(() => entry.WriteToDirectoryAsync(outputPath, options)))
                    .ToArray();

                await DependencyManager.uiLinker!.OpenLoadingModal(true, tasks);
            }
        }

        public class FolderEntry
        {
            public string? archiveFile;
            public string? extractedEntry;

            public string? selectedBinary;

            public FolderEntry(string? extract, string? extractedFolder)
            {
                this.archiveFile = extract;
                this.extractedEntry = extractedFolder;

                if (!string.IsNullOrEmpty(extractedEntry))
                    CrawlForExecutable(extractedEntry);
            }

            public void CrawlForExecutable(string root)
            {
                string[] files = Directory.GetFiles(root);
                IEnumerable<string> binaries = files.Where(IsExecutable);

                if (binaries?.Count() > 0)
                {
                    selectedBinary = binaries.FirstOrDefault();
                    return;
                }

                string[] subDirs = Directory.GetDirectories(root);

                foreach (string sub in subDirs)
                    CrawlForExecutable(sub);
            }
        }
    }
}

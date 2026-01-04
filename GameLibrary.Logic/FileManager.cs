using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using CSharpSqliteORM;
using GameLibrary.DB;
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

        public static async Task DeleteGameFiles(GameDto game)
        {
            if (Directory.Exists(game.getAbsoluteFolderLocation))
                Directory.Delete(game.getAbsoluteFolderLocation, true);
        }

        public static async Task UpdateGameIcon(int gameId, Uri newIconPath)
        {
            GameDto? game = LibraryHandler.TryGetCachedGame(gameId);

            if (!File.Exists(newIconPath.LocalPath) || game == null)
            {
                return;
            }

            string localPath = $"{Guid.NewGuid()}.png";
            File.Copy(newIconPath.LocalPath, Path.Combine(game.getAbsoluteFolderLocation, localPath));

            await game.UpdateGameIcon(localPath);
        }

        public static string CreateEmptyGameFolder(string absoluteBinaryFile)
        {
            string folderName = Path.Combine(Path.GetDirectoryName(absoluteBinaryFile)!, Path.GetFileName(absoluteBinaryFile).Replace(" ", string.Empty));

            Directory.CreateDirectory(folderName);
            Directory.Move(absoluteBinaryFile, folderName);

            return folderName;
        }

        public static Task MoveGameToItsLibrary(dbo_Game game, string binaryAbsolutePath, string libraryRootLocation)
        {
            string destination = Path.Combine(libraryRootLocation, game.gameFolder);
            string existingFolderPath = Path.GetDirectoryName(binaryAbsolutePath)!;

            try
            {
                Directory.Move(existingFolderPath, destination);
            }
            catch (IOException ex) when (ex.Message.Contains("Invalid cross-device link"))
            {
                CopyFiles(existingFolderPath, destination);
            }

            return Task.CompletedTask;
        }

        private static Task CopyFiles(string existing, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(existing))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(existing))
            {
                var destSubDir = Path.Combine(destination, Path.GetFileName(directory));
                CopyFiles(directory, destSubDir);
            }

            Directory.Delete(existing, recursive: true);
            return Task.CompletedTask;
        }


        public static async Task<List<IImportEntry>> CrawlGames(string[] paths)
        {
            List<IImportEntry> foundEntries = new List<IImportEntry>();

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
                ImportEntry_Folder folder = new ImportEntry_Folder(extract, extractedFolder >= 0 ? topLevelFolders[extractedFolder] : null);

                if (extractedFolder >= 0)
                    topLevelFolders.RemoveAt(extractedFolder);

                foundEntries.Add(folder);
            }

            foreach (string remainingFolder in topLevelFolders)
            {
                ImportEntry_Folder folder = new ImportEntry_Folder(null, remainingFolder);
                foundEntries.Add(folder);
            }

            return foundEntries;
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

            bool didExtract = false;
            string extractPath = Path.Combine(Path.GetDirectoryName(archiveFile)!, Path.GetFileNameWithoutExtension(archiveFile));

            ExtractionOptions extractionOptions = new ExtractionOptions()
            {
                ExtractFullPath = true,
                Overwrite = true,
            };

            Directory.CreateDirectory(extractPath);

            try
            {
                using (IArchive? archive = ArchiveFactory.Open(archiveFile))
                {
                    await DependencyManager.OpenLoadingModal(false, async () => archive.WriteToDirectory(extractPath, extractionOptions));
                    didExtract = true;
                }
            }
            catch (CryptographicException)
            {
                string? password = await DependencyManager.OpenStringInputModal("Archive Password") ?? string.Empty;

                using (IArchive? archive = ArchiveFactory.Open(archiveFile, new SharpCompress.Readers.ReaderOptions() { Password = password }))
                {
                    await DependencyManager.OpenLoadingModal(false, async () => archive.WriteToDirectory(extractPath, extractionOptions));
                    didExtract = true;
                }
            }
            catch
            {

            }

            if (didExtract)
            {
                return extractPath;
            }

            return string.Empty;

            //async Task Extract(IArchive archive, string outputPath, ExtractionOptions options)
            //{
            //    Func<Task>[] tasks = archive.Entries
            //        .Where(e => !e.IsDirectory)
            //        .Select(entry => (Func<Task>)(() => entry.WriteToDirectoryAsync(outputPath, options)))
            //        .ToArray();
            //
            //    await DependencyManager.OpenLoadingModal(true, tasks);
            //}
        }


        public interface IImportEntry
        {
            public string getPotentialName { get; }
            public string getBinaryPath { get; }

            public string? getBinaryFolder { get; }
        }

        public class ImportEntry_Folder : IImportEntry
        {
            public string? archiveFile;
            public string? extractedEntry;

            public string? selectedBinary;


            public string getPotentialName => Path.GetFileNameWithoutExtension(selectedBinary!).Replace(" ", string.Empty);
            public string getBinaryPath => selectedBinary!;
            public string? getBinaryFolder => extractedEntry;


            public ImportEntry_Folder(string? extract, string? extractedFolder)
            {
                this.archiveFile = extract?.Replace("%20", " ");
                this.extractedEntry = extractedFolder?.Replace("%20", " ");

                if (!string.IsNullOrEmpty(extractedEntry))
                    CrawlForExecutable(extractedEntry);
            }

            public void CrawlForExecutable(string root)
            {
                string[] files = Directory.GetFiles(root);
                IEnumerable<string> binaries = files.Where(RunnerManager.IsUniversallyAcceptedExecutableFormat);

                if (binaries?.Count() > 0)
                {
                    selectedBinary = TryFindBestExecutable(binaries);
                    return;
                }

                string[] subDirs = Directory.GetDirectories(root);

                foreach (string sub in subDirs)
                    CrawlForExecutable(sub);

                string TryFindBestExecutable(IEnumerable<string> possible)
                {
                    string? bestPossible = possible.FirstOrDefault(x =>
                    {
                        string name = Path.GetFileName(x).ToLower();
                        return !name.Contains("crash", StringComparison.InvariantCulture)
                                && !name.Contains("crash", StringComparison.InvariantCulture);
                    });

                    return bestPossible ?? possible.FirstOrDefault() ?? "";
                }
            }
        }

        public class ImportEntry_Binary : IImportEntry
        {
            public string binaryLocation;

            public string getPotentialName => Path.GetFileNameWithoutExtension(binaryLocation).Replace(" ", string.Empty);
            public string getBinaryPath => binaryLocation;
            public string? getBinaryFolder => null;

            public ImportEntry_Binary(string loc) => binaryLocation = loc;

        }
    }
}

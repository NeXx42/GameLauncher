using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic
{
    public static class FileManager
    {

        //public static void SaveScreenshot(obj bmp)
        //{
        //    Cleanup();
        //    bmp.Save(Path.Combine(GetTempLocation(), "temp.jpg"), ImageFormat.Jpeg);
        //}

        public static async Task<string> PromoteTempFile(int gameId, string path)
        {
            string extension = Path.GetExtension(path);
            string newName = $"{Guid.NewGuid()}{extension}";

            string folderName = await LibraryHandler.GetGameFromId(gameId)!.GetFolderLocation();
            File.Move(path, Path.Combine(folderName, newName));
            return newName;
        }

        public static async Task<(bool isInvalid, bool wasMigrated)> TryMigrate(dbo_Game game)
        {
            if (!File.Exists(game.executablePath))
            {
                return (true, false);
            }

            try
            {
                string parentExecutableFolder = Path.GetDirectoryName(game.executablePath)!;

                Directory.Move(parentExecutableFolder, await game.GetFolderLocation());
                game.executablePath = $"{Path.GetFileName(game.executablePath)}";

                return (false, true);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                return (true, false);
            }
        }

        public static async Task BrowseToGame(dbo_Game game)
        {
            string folder = Path.GetDirectoryName(await game.GetExecutableLocation()) ?? string.Empty;

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;

            Process.Start("explorer.exe", folder);
        }

        public static async Task<Exception?> DeleteGame(dbo_Game game)
        {
            try
            {
                string toDelete = await game.GetFolderLocation();

                if (!string.IsNullOrEmpty(toDelete) && Directory.Exists(toDelete))
                    Directory.Delete(toDelete, true);

                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }



        public static async Task<List<GameFolder>> CrawlGames(string[] paths)
        {
            List<GameFolder> foundGameFolders = new List<GameFolder>();
            List<GameZip> foundZips = new List<GameZip>();

            foreach (string path in paths)
                Craw(path);

            return foundGameFolders;

            void Craw(string path)
            {
                string[] allFiles = Directory.GetFiles(path);
                string[] binaries = allFiles.Where(x => x.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)).ToArray();

                if (binaries.Length > 0)
                {
                    foundGameFolders.Add(new GameFolder()
                    {
                        path = path,
                        executables = binaries
                    });
                    return;
                }

                string[] zips = allFiles.Where(IsZip).ToArray();

                if (zips.Length > 0)
                {
                    foreach (string zip in zips)
                    {
                        foundZips.Add(new GameZip()
                        {
                            path = zip
                        });
                    }
                }

                string[] subDirs = Directory.GetDirectories(path);

                foreach (string dir in subDirs)
                {
                    Craw(dir);
                }
            }

        }

        private static bool IsZip(string path)
        {
            return path.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".rar", StringComparison.InvariantCultureIgnoreCase);
        }


        public struct GameFolder
        {
            public string path;
            public string[] executables;
        }

        private struct GameZip
        {
            public string path;
        }
    }
}

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;

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

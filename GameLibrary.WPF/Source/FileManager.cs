using GameLibary.Source.Database.Tables;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace GameLibary.Source
{
    public static class FileManager
    {
        public static string GetDataLocation() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyLibaryApplication");
        public static string GetTempLocation() => Path.Combine(GetDataLocation(), "__Temp");


        public static void Setup()
        {
            string root = GetDataLocation();

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            string temp = GetTempLocation();

            if (!Directory.Exists(temp))
                Directory.CreateDirectory(temp);
        }

        public static void Cleanup()
        {
            foreach (string f in Directory.GetFiles(GetTempLocation()))
            {
                File.Delete(f);
            }
        }

        public static void SaveScreenshot(Bitmap bmp)
        {
            Cleanup();
            bmp.Save(Path.Combine(GetTempLocation(), "temp.jpg"), ImageFormat.Jpeg);
        }

        public static bool GetTempScreenshot(out string path)
        {
            path = Directory.GetFiles(GetTempLocation()).FirstOrDefault() ?? "";
            return !string.IsNullOrEmpty(path);
        }

        public static async Task<string> PromoteTempFile(int gameId, string path)
        {
            string extension = Path.GetExtension(path);
            string newName = $"{Guid.NewGuid()}{extension}";

            string folderName = await LibaryHandler.GetGameFromId(gameId)!.GetFolderLocation();
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
                MessageBox.Show(e.Message);
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
    }
}

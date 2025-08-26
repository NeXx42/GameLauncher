using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using GameLibary.Source.Database.Tables;
using Microsoft.Win32.SafeHandles;

namespace GameLibary.Source
{
    public static class FileManager
    {
        public static string GetDataLocation() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyLibaryApplication");
        public static string GetTempLocation() => Path.Combine(GetDataLocation(), "__Temp");

        public static string GetProcessGameLocation() => Path.Combine(MainWindow.GameRootLocation, "____Processed");

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
            foreach(string f in Directory.GetFiles(GetTempLocation()))
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

        public static string PromoteTempFile(int gameId, string path)
        {
            string extension = Path.GetExtension(path);
            string newName = $"{Guid.NewGuid()}{extension}";

            File.Move(path, Path.Combine(LibaryHandler.GetGameFromId(gameId).GetFolderName, newName));
            return newName;
        }

        public static async Task<(bool isInvalid, bool wasMigrated)> TryMigrate(dbo_Game game)
        {
            if (game.executablePath.StartsWith("#"))
            {
                return (false, false);
            }

            if(!File.Exists(game.executablePath))
            {
                return (true, false);
            }

            if(!Directory.Exists(GetProcessGameLocation()))
            {
                Directory.CreateDirectory(GetProcessGameLocation());
            }

            try
            {
                string parentExecutableFolder = Path.GetDirectoryName(game.executablePath);

                Directory.Move(parentExecutableFolder, Path.Combine(GetProcessGameLocation(), $"{game.gameName}"));
                game.executablePath =$"#{Path.GetFileName(game.executablePath)}";

                return (false, true);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return (true, false);
            }

        }
    }
}

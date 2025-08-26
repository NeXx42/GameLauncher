using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string newName = $"{gameId}_{Guid.NewGuid()}{extension}";

            File.Move(path, GetPathForScreenshot(newName));
            return newName;
        }

        public static string GetPathForScreenshot(string fileName) => Path.Combine(GetDataLocation(), fileName);
    }
}

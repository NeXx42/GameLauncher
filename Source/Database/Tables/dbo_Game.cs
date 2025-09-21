using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GameLibary.Source.Database.Tables
{
    public class dbo_Game : DatabaseTable
    {
        public override string tableName => "Games";

        public int id { get; set; }

        public string? gameName { get; set; }
        public string? executablePath { get; set; }
        public string? iconPath { get; set; }
        public bool useEmulator {  get; set; }
        public DateTime? lastPlayed { get; set; }


        private BitmapImage? cachedImage;


        public string GetFolderName => Path.Combine(FileManager.GetProcessGameLocation(), gameName);
        public string GetRealIconPath => !string.IsNullOrEmpty(iconPath) ? Path.Combine(GetFolderName, iconPath) : "";
        public string GetRealExecutionPath => !string.IsNullOrEmpty(executablePath) ? Path.Combine(GetFolderName, executablePath.Substring(1)) : "";


        public BitmapImage? GetCachedImage() => cachedImage;
        public void SetCachedIcon(BitmapImage? to) => cachedImage = to;


        public override Row[] GetRows() => new[]
        {
            new Row() {  name = nameof(id), type = DataType.INTEGER, isPrimaryKey = true, isAutoIncrement = true },

            new Row() {  name = nameof(gameName), type = DataType.TEXT },
            new Row() {  name = nameof(executablePath), type = DataType.TEXT },
            new Row() {  name = nameof(iconPath), type = DataType.TEXT },
            new Row() {  name = nameof(useEmulator), type = DataType.BIT },

            new Row() {  name = nameof(lastPlayed), type = DataType.DATETIME, isNullable = true },
        };
    }
}

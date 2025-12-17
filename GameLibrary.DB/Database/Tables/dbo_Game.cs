using System.IO;

namespace GameLibrary.DB.Tables
{
    public class dbo_Game : Database_Table
    {
        public override string tableName => "Games";

        public int id { get; set; }

        public required string gameName { get; set; }
        public string? executablePath { get; set; }
        public string? iconPath { get; set; }
        public bool useEmulator { get; set; }
        public DateTime? lastPlayed { get; set; }
        public required int libaryId { get; set; }
        public required string gameFolder { get; set; }
        public int? wineProfile { get; set; }

        //public async Task<string> GetLibraryLocation() => (await DatabaseHandler.GetItem<dbo_Libraries>(QueryBuilder.SQLEquals(nameof(dbo_Libraries.libaryId), libaryId)))?.rootPath ?? string.Empty;
        //public async Task<string> GetAbsoluteFolderLocation() => Path.Combine(await GetLibraryLocation(), gameFolder);
        //public async Task<string> GetAbsoluteExecutableLocation() => !string.IsNullOrEmpty(executablePath) ? Path.Combine(await GetAbsoluteFolderLocation(), executablePath) : "";
        //
        //public async Task<string> GetAbsoluteIconLocation() => !string.IsNullOrEmpty(iconPath) ? Path.Combine(await GetAbsoluteFolderLocation(), iconPath) : "";


        public override Row[] GetRows() => new[]
        {
            new Row() {  name = nameof(id), type = DataType.INTEGER, isPrimaryKey = true, isAutoIncrement = true },

            new Row() {  name = nameof(gameName), type = DataType.TEXT, isNullable = false },
            new Row() {  name = nameof(executablePath), type = DataType.TEXT },
            new Row() {  name = nameof(iconPath), type = DataType.TEXT },
            new Row() {  name = nameof(useEmulator), type = DataType.BIT },

            new Row() {  name = nameof(lastPlayed), type = DataType.DATETIME, isNullable = true },
            new Row() {  name = nameof(libaryId), type = DataType.INTEGER, isNullable = false },

            new Row() {  name = nameof(gameFolder), type = DataType.TEXT, isNullable = true },

            new Row() {  name = nameof(wineProfile), type = DataType.INTEGER, isNullable = true },
        };
    }
}

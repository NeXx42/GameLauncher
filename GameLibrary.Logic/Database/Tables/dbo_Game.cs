using System.IO;
using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Tables
{
    public class dbo_Game : IDatabase_Table
    {
        public static string tableName => "Games";

        public int id { get; set; }

        public required string gameName { get; set; }
        public string? executablePath { get; set; }
        public string? iconPath { get; set; }
        public bool useEmulator { get; set; }
        public DateTime? lastPlayed { get; set; }
        public required int libaryId { get; set; }
        public required string gameFolder { get; set; }
        public bool? captureLogs { get; set; }
        public int? runnerId { get; set; }

        //public async Task<string> GetLibraryLocation() => (await DatabaseHandler.GetItem<dbo_Libraries>(QueryBuilder.SQLEquals(nameof(dbo_Libraries.libaryId), libaryId)))?.rootPath ?? string.Empty;
        //public async Task<string> GetAbsoluteFolderLocation() => Path.Combine(await GetLibraryLocation(), gameFolder);
        //public async Task<string> GetAbsoluteExecutableLocation() => !string.IsNullOrEmpty(executablePath) ? Path.Combine(await GetAbsoluteFolderLocation(), executablePath) : "";
        //
        //public async Task<string> GetAbsoluteIconLocation() => !string.IsNullOrEmpty(iconPath) ? Path.Combine(await GetAbsoluteFolderLocation(), iconPath) : "";


        public static Database_Column[] getColumns => new[]
        {
            new Database_Column() {  columnName = nameof(id), columnType = Database_ColumnType.INTEGER, isPrimaryKey = true, autoIncrement = true },

            new Database_Column() {  columnName = nameof(gameName), columnType = Database_ColumnType.TEXT, allowNull = false },
            new Database_Column() {  columnName = nameof(executablePath), columnType = Database_ColumnType.TEXT },
            new Database_Column() {  columnName = nameof(iconPath), columnType = Database_ColumnType.TEXT },
            new Database_Column() {  columnName = nameof(useEmulator), columnType = Database_ColumnType.BIT },

            new Database_Column() {  columnName = nameof(lastPlayed), columnType = Database_ColumnType.DATETIME, allowNull = true },
            new Database_Column() {  columnName = nameof(libaryId), columnType = Database_ColumnType.INTEGER, allowNull = false },

            new Database_Column() {  columnName = nameof(gameFolder), columnType = Database_ColumnType.TEXT, allowNull = true },

            new Database_Column() {  columnName = nameof(captureLogs), columnType = Database_ColumnType.BIT, allowNull = true },

            new Database_Column() {  columnName = nameof(runnerId), columnType = Database_ColumnType.INTEGER, allowNull = true },
        };
    }
}

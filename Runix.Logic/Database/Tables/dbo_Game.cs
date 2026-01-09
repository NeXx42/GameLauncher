using System.IO;
using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Tables
{
    public class dbo_Game : IDatabase_Table
    {
        public static string tableName => "Games";

        public int id { get; set; }

        public required string gameName { get; set; }
        public required string gameFolder { get; set; }
        public string? executablePath { get; set; }
        public string? iconPath { get; set; }

        public int? libraryId { get; set; }
        public int? runnerId { get; set; }
        public required int status { get; set; }

        public int? minsPlayed { get; set; }
        public DateTime? lastPlayed { get; set; }

        public static Database_Column[] getColumns => new[]
        {
            new Database_Column() {  columnName = nameof(id), columnType = Database_ColumnType.INTEGER, isPrimaryKey = true, autoIncrement = true },

            new Database_Column() {  columnName = nameof(gameName), columnType = Database_ColumnType.TEXT, allowNull = false },
            new Database_Column() {  columnName = nameof(iconPath), columnType = Database_ColumnType.TEXT },
            new Database_Column() {  columnName = nameof(gameFolder), columnType = Database_ColumnType.TEXT, allowNull = true },
            new Database_Column() {  columnName = nameof(executablePath), columnType = Database_ColumnType.TEXT },

            new Database_Column() {  columnName = nameof(libraryId), columnType = Database_ColumnType.INTEGER, allowNull = true },
            new Database_Column() {  columnName = nameof(runnerId), columnType = Database_ColumnType.INTEGER, allowNull = true },
            new Database_Column() {  columnName = nameof(status), columnType = Database_ColumnType.INTEGER, allowNull = false, defaultValue = "1" },

            new Database_Column() {  columnName = nameof(minsPlayed), columnType = Database_ColumnType.INTEGER, allowNull = true},
            new Database_Column() {  columnName = nameof(lastPlayed), columnType = Database_ColumnType.DATETIME, allowNull = true},
        };
    }
}

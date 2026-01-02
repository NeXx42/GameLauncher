using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Tables
{
    public class dbo_Libraries : IDatabase_Table
    {
        public static string tableName => "Libraries";

        public int libaryId { get; set; }
        public required string rootPath { get; set; }
        public int libraryExternalType { get; set; }

        public static Database_Column[] getColumns => [
            new Database_Column(){ columnName = nameof(libaryId), columnType = Database_ColumnType.INTEGER, allowNull = false, autoIncrement = true, isPrimaryKey = true },
            new Database_Column(){ columnName = nameof(rootPath), columnType = Database_ColumnType.TEXT, allowNull = false },
            new Database_Column(){ columnName = nameof(libraryExternalType), columnType = Database_ColumnType.INTEGER, allowNull = true },
        ];
    }
}

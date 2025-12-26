using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Tables
{
    public class dbo_Tag : IDatabase_Table
    {
        public static string tableName => "Tag";

        public int TagId { get; set; }
        public string TagName { get; set; }
        public string TagHexColour { get; set; }

        public static Database_Column[] getColumns => [
            new Database_Column(){ columnName = nameof(TagId), autoIncrement = true, allowNull = false, columnType = Database_ColumnType.INTEGER, isPrimaryKey = true },
            new Database_Column(){ columnName = nameof(TagName), columnType = Database_ColumnType.TEXT, allowNull = false },
            new Database_Column(){ columnName = nameof(TagHexColour), columnType = Database_ColumnType.TEXT }
        ];
    }
}

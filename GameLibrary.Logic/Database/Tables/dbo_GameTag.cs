using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Tables
{
    public class dbo_GameTag : IDatabase_Table
    {
        public static string tableName => "GameTag";

        public int GameId { get; set; }
        public int TagId { get; set; }

        public static Database_Column[] getColumns => [
            new Database_Column(){ columnName = nameof(GameId), columnType = Database_ColumnType.INTEGER, allowNull = false},
            new Database_Column(){ columnName = nameof(TagId), columnType = Database_ColumnType.INTEGER, allowNull = false},
        ];
    }
}

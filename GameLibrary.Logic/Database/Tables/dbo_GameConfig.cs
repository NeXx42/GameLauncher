using CSharpSqliteORM.Structure;

namespace GameLibrary.Logic.Database.Tables;

public class dbo_GameConfig : IDatabase_Table
{
    public static string tableName => "GameConfig";

    public required int gameId { set; get; }
    public required string configKey { set; get; }
    public string? configValue { set; get; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(gameId),  columnType = Database_ColumnType.INTEGER, allowNull = false },

        new Database_Column() { columnName = nameof(configKey),  columnType = Database_ColumnType.TEXT, allowNull = false },
        new Database_Column() { columnName = nameof(configValue),  columnType = Database_ColumnType.TEXT, allowNull = true },
    ];
}

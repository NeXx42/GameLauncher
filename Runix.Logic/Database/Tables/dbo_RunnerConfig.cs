using CSharpSqliteORM.Structure;

namespace GameLibrary.Logic.Database.Tables;

public class dbo_RunnerConfig : IDatabase_Table
{
    public static string tableName => "RunnerConfig";

    public required int runnerId { get; set; }
    public required string settingKey { get; set; }
    public string? settingValue { set; get; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(runnerId), columnType = Database_ColumnType.INTEGER, allowNull = false },
        new Database_Column() { columnName = nameof(settingKey), columnType = Database_ColumnType.TEXT, allowNull = false },
        new Database_Column() { columnName = nameof(settingValue), columnType = Database_ColumnType.TEXT }
    ];
}

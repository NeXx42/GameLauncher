using CSharpSqliteORM.Structure;

namespace GameLibrary.Logic.Database.Tables;

public class dbo_Runner : IDatabase_Table
{
    public static string tableName => "Runner";

    public required int runnerId { get; set; }
    public required string runnerName { get; set; }
    public required int runnerType { get; set; }
    public required string runnerRoot { get; set; }
    public required string runnerVersion { get; set; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(runnerId), columnType = Database_ColumnType.INTEGER, allowNull = false, isPrimaryKey = true, autoIncrement = true },
        new Database_Column() { columnName = nameof(runnerName), columnType = Database_ColumnType.TEXT, allowNull = false },
        new Database_Column() { columnName = nameof(runnerType), columnType = Database_ColumnType.INTEGER, allowNull = false },

        new Database_Column() { columnName = nameof(runnerRoot), columnType = Database_ColumnType.TEXT, allowNull = false },
        new Database_Column() { columnName = nameof(runnerVersion), columnType = Database_ColumnType.TEXT, allowNull = false },
    ];
}

using CSharpSqliteORM.Structure;

namespace GameLibrary.DB.Database.Tables;

public class dbo_WineProfile : IDatabase_Table
{
    public static string tableName => "WineProfiles";

    public int id { get; set; }
    public string? profileName { get; set; }
    public string? profileDirectory { get; set; }
    public string? profileExecutable { get; set; }
    public bool isDefault { get; set; }
    public required int emulatorType { get; set; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(id), autoIncrement = true, allowNull = false, isPrimaryKey = true, columnType = Database_ColumnType.INTEGER },
        new Database_Column() { columnName = nameof(profileName), columnType = Database_ColumnType.TEXT },
        new Database_Column() { columnName = nameof(profileDirectory), columnType = Database_ColumnType.TEXT },
        new Database_Column() { columnName = nameof(profileExecutable), columnType = Database_ColumnType.TEXT },
        new Database_Column() { columnName = nameof(isDefault), columnType = Database_ColumnType.BIT },
        new Database_Column() { columnName = nameof(emulatorType), columnType = Database_ColumnType.INTEGER, allowNull = false },
    ];
}

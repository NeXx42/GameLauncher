namespace GameLibrary.DB.Database.Tables;

public class dbo_WineProfile : Database_Table
{
    public override string tableName => "WineProfiles";

    public int id { get; set; }
    public string? profileName { get; set; }
    public string? profileDirectory { get; set; }
    public string? profileExecutable { get; set; }
    public bool isDefault { get; set; }
    public required int emulatorType { get; set; }

    public override Row[] GetRows() => [
        new Row() { name = nameof(id), isAutoIncrement = true, isNullable = false, isPrimaryKey = true, type = DataType.INTEGER },
        new Row() { name = nameof(profileName), type = DataType.TEXT },
        new Row() { name = nameof(profileDirectory), type = DataType.TEXT },
        new Row() { name = nameof(profileExecutable), type = DataType.TEXT },
        new Row() { name = nameof(isDefault), type = DataType.BIT },
        new Row() { name = nameof(emulatorType), type = DataType.INTEGER, isNullable = false },
    ];
}

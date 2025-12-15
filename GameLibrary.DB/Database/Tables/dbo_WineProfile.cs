namespace GameLibrary.DB.Database.Tables;

public class dbo_WineProfile : Database_Table
{
    public override string tableName => "dbo_WineProfiles";

    public int id { get; set; }
    public string? profileName { get; set; }
    public string? profileDirectory { get; set; }

    public override Row[] GetRows() => [
        new Row() { name = nameof(id), isAutoIncrement = true, isNullable = false, isPrimaryKey = true, type = DataType.INTEGER },
        new Row() { name = nameof(profileName), type = DataType.TEXT },
        new Row() { name = nameof(profileDirectory), type = DataType.TEXT },
    ];
}

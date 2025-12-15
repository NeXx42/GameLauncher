using GameLibrary.DB.Tables;

namespace GameLibrary.DB.Database.Migrations;

public class Migration_WineProfile : Database_MigrationBase
{
    public override long migrationId => new DateTime(2025, 12, 15, 10, 51, 15, DateTimeKind.Utc).Ticks;

    public override string Up()
    {
        dbo_Game template = new dbo_Game() { gameName = "", libaryId = 0, gameFolder = "" };
        string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.wineProfile))!);

        return $"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL};";
    }

    public override string Down()
    {
        throw new NotImplementedException();
    }
}

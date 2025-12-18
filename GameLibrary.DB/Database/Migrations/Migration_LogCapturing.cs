using System.Text;
using GameLibrary.DB.Tables;

namespace GameLibrary.DB.Database.Migrations;

public class Migration_LogCapturing : Database_MigrationBase
{
    public override long migrationId => new DateTime(2025, 12, 18, 19, 58, 15, DateTimeKind.Utc).Ticks;

    public override string Up()
    {
        dbo_Game template = new dbo_Game() { gameFolder = "", libaryId = 0, gameName = "" };
        string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.captureLogs))!);

        StringBuilder sb = new StringBuilder($"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL};");
        return sb.ToString();
    }

    public override string Down()
    {
        throw new NotImplementedException();
    }
}

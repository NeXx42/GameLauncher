using GameLibrary.DB.Tables;

namespace GameLibrary.DB.Migrations
{
    internal class Migration_Sorting : MigrationBase
    {
        public override long migrationId => new DateTime(2025, 09, 21, 12, 30, 15, DateTimeKind.Utc).Ticks;

        public override string Down()
        {
            throw new NotImplementedException();
        }

        public override string Up()
        {
            dbo_Game template = new dbo_Game() { gameName = "", libaryId = 0, gameFolder = "" };
            string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.lastPlayed))!);

            return $"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL};";
        }
    }
}

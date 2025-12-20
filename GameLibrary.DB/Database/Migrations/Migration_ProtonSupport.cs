using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;

namespace GameLibrary.DB.Migrations
{
    internal class Migration_ProtonSupport : Database_MigrationBase
    {
        public override long migrationId => new DateTime(2025, 12, 20, 15, 48, 15, DateTimeKind.Utc).Ticks;

        public override string Down()
        {
            throw new NotImplementedException();
        }

        public override string Up()
        {
            dbo_WineProfile template = new dbo_WineProfile() { emulatorType = 0 };
            string emulatorColumn = template.BuildRowCreation(template.GetRow(nameof(template.emulatorType))!);
            string protonColumn = template.BuildRowCreation(template.GetRow(nameof(template.profileExecutable))!);
            string defaultColumn = template.BuildRowCreation(template.GetRow(nameof(template.isDefault))!);

            return $"ALTER TABLE {template.tableName} ADD COLUMN {protonColumn}; ALTER TABLE {template.tableName} ADD COLUMN {emulatorColumn} DEFAULT 0; ALTER TABLE {template.tableName} ADD COLUMN {defaultColumn};";
        }
    }
}

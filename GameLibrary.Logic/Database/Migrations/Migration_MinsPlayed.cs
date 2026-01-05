using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_MinsPlayed : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 1, 5, 18, 49, 10).Ticks;

    public string Up()
    {
        return @$"
            ALTER TABLE {dbo_Game.tableName} ADD COLUMN minsPlayed Integer Nullable;
        ";
    }
}

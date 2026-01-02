using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_GameStatus : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 1, 2, 19, 56, 10).Ticks;

    public string Up()
    {
        StringBuilder sb = new StringBuilder($"ALTER TABLE {dbo_Game.tableName} ADD COLUMN ");
        sb.Append(dbo_Game.getColumns.First(x => x.columnName.Equals(nameof(dbo_Game.status))).GenerateColumnSQL());

        return sb.ToString();
    }
}

using System.Text;
using CSharpSqliteORM;
using CSharpSqliteORM.Structure;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_NewRunnerSystem : IDatabase_Migration
{
    public long migrationId => new DateTime(2025, 12, 26, 21, 28, 10).Ticks;

    public string Up()
    {
        StringBuilder sb = new StringBuilder($"Alter table {dbo_Game.tableName} ADD COLUMN ");
        sb.Append(dbo_Game.getColumns.First(x => x.columnName.Equals(nameof(dbo_Game.runnerId))).GenerateColumnSQL());

        return sb.ToString();
    }
}

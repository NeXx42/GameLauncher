using System.Text;
using CSharpSqliteORM;
using CSharpSqliteORM.Structure;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_VirtualLibraries : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 1, 1, 18, 7, 10).Ticks;

    public string Up()
    {
        StringBuilder sb = new StringBuilder($"ALTER TABLE {dbo_Game.tableName} RENAME TO _temp;");
        sb.Append(Database_ColumnMapper.CreateTable<dbo_Game>());
        sb.Append(";");

        string cols = string.Join(",", dbo_Game.getColumns.Select(x => x.columnName).ToArray());
        sb.Append($"INSERT INTO {dbo_Game.tableName} ({string.Join(",", cols)}) SELECT {cols.Replace("libraryId", "libaryId")} FROM _temp;");

        string str = sb.ToString();
        return str;
    }
}

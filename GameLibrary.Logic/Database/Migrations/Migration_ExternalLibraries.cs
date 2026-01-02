using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_ExternalLibraries : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 1, 2, 12, 44, 10).Ticks;

    public string Up()
    {
        StringBuilder sb = new StringBuilder($"ALTER TABLE {dbo_Libraries.tableName} ADD COLUMN ");
        sb.Append(dbo_Libraries.getColumns.First(x => x.columnName.Equals(nameof(dbo_Libraries.libraryExternalType))).GenerateColumnSQL());

        return sb.ToString();
    }
}

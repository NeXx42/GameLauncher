using GameLibary.Source.Database.Tables;
using System.Text;

namespace GameLibary.Source.Database.Migrations
{
    public class Migration_SeparateLibraries : MigrationBase
    {
        public override long migrationId => new DateTime(2025, 10, 7, 16, 17, 15, DateTimeKind.Utc).Ticks;

        public override string Up()
        {
            StringBuilder sql = new StringBuilder();

            dbo_Game template = new dbo_Game() { gameName = "", libaryId = 0, gameFolder = "" };
            string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.libaryId))!);

            sql.Append($"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL} DEFAULT 1;");

            dbo_Config conf = new dbo_Config();
            dbo_Libraries lib = new dbo_Libraries() { rootPath = "" };

            sql.Append($"INSERT INTO {lib.tableName} ({nameof(lib.rootPath)}) SELECT {nameof(conf.value)} from {conf.tableName} WHERE {nameof(conf.key)} = 'RootPath' LIMIT 1;"); // insert old root path as new libary
            sql.Append($"DELETE FROM {conf.tableName} WHERE {nameof(conf.key)} = 'RootPath';"); // delete old root path
            sql.Append($"UPDATE {template.tableName} SET {nameof(template.executablePath)} = substr({nameof(template.executablePath)}, 2) WHERE {nameof(template.executablePath)} LIKE '#%'"); // clean old

            return sql.ToString();
        }


        public override string Down()
        {
            throw new NotImplementedException();
        }
    }
}

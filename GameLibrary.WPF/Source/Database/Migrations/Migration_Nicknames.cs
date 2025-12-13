using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database.Migrations
{
    internal class Migration_Nicknames : MigrationBase
    {
        public override long migrationId => new DateTime(2025, 11, 22, 16, 03, 15, DateTimeKind.Utc).Ticks;

        public override string Down()
        {
            throw new NotImplementedException();
        }

        public override string Up()
        {
            dbo_Game template = new dbo_Game() { gameFolder = "", libaryId = 0, gameName = "" };
            string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.gameFolder))!);

            StringBuilder sb = new StringBuilder($"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL};");
            sb.Append($"UPDATE {template.tableName} SET gameFolder = gameName");

            return sb.ToString();
        }
    }
}

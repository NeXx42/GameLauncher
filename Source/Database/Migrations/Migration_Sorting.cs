using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database.Migrations
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
            dbo_Game template = new dbo_Game();
            string rowSQL = template.BuildRowCreation(template.GetRow(nameof(template.lastPlayed))!);

            return $"ALTER TABLE {template.tableName} ADD COLUMN {rowSQL};";
        }
    }
}

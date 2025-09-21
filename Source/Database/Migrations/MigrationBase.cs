using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database.Migrations
{
    public abstract class MigrationBase
    {
        public const string CONFIG_MIGRATIONID = "MIGRATIONID";

        public abstract long migrationId { get; }

        public abstract string Up();
        public abstract string Down();
    }
}

namespace GameLibrary.DB
{
    public abstract class Database_MigrationBase
    {
        public const string CONFIG_MIGRATIONID = "MIGRATIONID";

        public abstract long migrationId { get; }

        public abstract string Up();
        public abstract string Down();
    }
}

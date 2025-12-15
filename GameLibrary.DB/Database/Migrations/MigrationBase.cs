namespace GameLibrary.DB.Migrations
{
    public abstract class MigrationBase
    {
        public const string CONFIG_MIGRATIONID = "MIGRATIONID";

        public abstract long migrationId { get; }

        public abstract string Up();
        public abstract string Down();
    }
}

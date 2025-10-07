using GameLibary.Source.Database;
using GameLibary.Source.Database.Migrations;
using GameLibary.Source.Database.Tables;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace GameLibary.Source
{
    public static class DatabaseHandler
    {
        private static string dbPath => Path.Combine(FileManager.GetDataLocation(), "libary.db");
        private static string GetConnectionString() => $"Data Source={dbPath};Version=3;";

        private static SQLiteConnection connection;


        public static void Setup()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                connection = new SQLiteConnection(GetConnectionString());
            }

            connection ??= new SQLiteConnection(GetConnectionString());
            GenerateTables();
            HandleMigrations();
        }

        private static void GenerateTables()
        {
            // cannot add or modify existing columns. way too advanced for this

            Type[] tables = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(DatabaseTable))).ToArray();

            connection.Open();

            foreach (Type tableType in tables)
            {
                DatabaseTable? table = Activator.CreateInstance(tableType) as DatabaseTable;

                using (SQLiteCommand command = new SQLiteCommand(table!.GenerateCreateCommand(), connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            connection.Close();
        }

        private static async void HandleMigrations()
        {
            Type[] migrations = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MigrationBase))).ToArray();

            long? lastMigration = null;
            string? id = (await GetItem<dbo_Config>(QueryBuilder.SQLEquals(nameof(dbo_Config.key), MigrationBase.CONFIG_MIGRATIONID)))?.value ?? null;

            if (!string.IsNullOrEmpty(id))
            {
                lastMigration = long.Parse(id);
            }

            MigrationBase[] migrationsToApply = migrations.Select(x => (MigrationBase)Activator.CreateInstance(x)!)
                .Where(x => x.migrationId > (lastMigration ?? 0))
                .OrderBy(x => x.migrationId).ToArray();

            if (lastMigration.HasValue)
            {
                lastMigration = null;

                connection.Open();

                foreach (MigrationBase migration in migrationsToApply)
                {
                    lastMigration = migration.migrationId;

                    using (SQLiteCommand command = new SQLiteCommand(migration.Up(), connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                connection.Close();
            }
            else
            {
                // migrations in this context are only to update existing database TABLES,
                // as migrations are only for amending tables there is no need to do migration on a database that is has just been created

                lastMigration = migrationsToApply[migrations.Length - 1].migrationId;
            }

            if (lastMigration.HasValue)
            {
                await DeleteFromTable<dbo_Config>(QueryBuilder.SQLEquals(nameof(dbo_Config.key), MigrationBase.CONFIG_MIGRATIONID));
                await InsertIntoTable(new dbo_Config() { key = MigrationBase.CONFIG_MIGRATIONID, value = lastMigration.Value.ToString() });
            }
        }


        public static async Task InsertIntoTable(DatabaseTable value)
        {
            await TryExecute(value.GenerateInsertCommand());
        }

        public static async Task DeleteFromTable<T>(QueryBuilder.InternalAccessor queryBuilder) where T : DatabaseTable
        {
            await TryExecute($"DELETE FROM {GetTableNameFromGeneric<T>()} {queryBuilder?.BuildWhereClause() ?? ""}");
        }

        public static async Task UpdateTableEntry<T>(T entry, QueryBuilder.InternalAccessor queryBuilder) where T : DatabaseTable
        {
            string updateSQL = entry.GenerateUpdateCommand();
            await TryExecute($"{updateSQL} {queryBuilder?.BuildWhereClause() ?? ""}");
        }


        private static async Task TryExecute(string sql)
        {
            try
            {
                connection.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"{sql}\n{e.Message}", "Failed sql", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }


        public static async Task<bool> Exists<T>(QueryBuilder.InternalAccessor? queryBuilder = null) where T : DatabaseTable
        {
            try
            {
                bool exists = false;
                connection.Open();

                StringBuilder sql = new StringBuilder($"SELECT 1 FROM {GetTableNameFromGeneric<T>()}");

                if (queryBuilder != null)
                    sql.Append($" {queryBuilder?.BuildWhereClause()}");

                sql.Append(" LIMIT 1;");

                using (SQLiteCommand cmd = new SQLiteCommand(sql.ToString(), connection))
                using (SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    exists = reader.HasRows;
                }

                connection.Close();
                return exists;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to get SQL");
                return false;
            }
        }


        public static async Task<T?> GetItem<T>(QueryBuilder.InternalAccessor? queryBuilder = null) where T : DatabaseTable
            => (await GetItems<T>(queryBuilder, 1)).FirstOrDefault();

        public static async Task<T[]> GetItems<T>(QueryBuilder.InternalAccessor? queryBuilder = null, int? limit = null) where T : DatabaseTable
        {
            try
            {
                List<T> val = new List<T>(limit ?? 10);
                connection.Open();

                StringBuilder sql = new StringBuilder($"SELECT * FROM {GetTableNameFromGeneric<T>()}");

                if (queryBuilder != null)
                    sql.Append($" {queryBuilder?.BuildWhereClause()}");

                if (limit.HasValue)
                    sql.Append($" LIMIT {limit.Value}");

                sql.Append(";");

                using (SQLiteCommand cmd = new SQLiteCommand(sql.ToString(), connection))
                using (SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        T res = (T)Activator.CreateInstance(typeof(T))!;
                        res.Map(reader);

                        val.Add(res);
                    }
                }

                connection.Close();
                return val.ToArray();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to get SQL");
                return Array.Empty<T>();
            }
        }

        private static string GetTableNameFromGeneric<T>() where T : DatabaseTable
        {
            T table = (T)Activator.CreateInstance(typeof(T))!;
            return table.tableName;
        }
    }



    public static class QueryBuilder
    {
        public static InternalAccessor SQLEquals(string column, string value)
            => new InternalAccessor().SQLEquals(column, value);

        public static InternalAccessor SQLEquals(string column, int value)
            => new InternalAccessor().SQLEquals(column, value);

        public static InternalAccessor In(string column, params int[] values)
            => new InternalAccessor().In(column, values);

        public class InternalAccessor
        {
            private string searchClause = "";

            private void PrepSearch()
            {
                if (!string.IsNullOrEmpty(searchClause))
                    searchClause += " AND";
            }

            public InternalAccessor SQLEquals(string column, string value)
            {
                PrepSearch();

                searchClause += $" {column} = '{value}'";
                return this;
            }

            public InternalAccessor SQLEquals(string column, int value)
            {
                PrepSearch();

                searchClause += $" {column} = {value}";
                return this;
            }

            public InternalAccessor In(string column, params int[] values)
            {
                PrepSearch();

                searchClause += $" {column} in ( ";

                for (int i = 0; i < values.Length; i++)
                {
                    searchClause += $"{values[i]}";

                    if (i < values.Length - 1)
                        searchClause += ",";
                }
                searchClause += ")";
                return this;
            }

            public string BuildWhereClause()
            {
                return string.IsNullOrEmpty(searchClause) ? "" : $"WHERE {searchClause}";
            }
        }
    }
}

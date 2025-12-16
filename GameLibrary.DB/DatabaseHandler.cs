using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using GameLibrary.DB.Migrations;
using GameLibrary.DB.Tables;

namespace GameLibrary.DB
{
    public static class DatabaseHandler
    {

        private static string? dbPath;
        private static string GetConnectionString() => $"Data Source={dbPath};Version=3;";

        private static SQLiteConnection? connection;

        private static Func<Exception, Task>? errorCallback;

        public static async Task Setup(string dbPath, Func<Exception, Task>? errorCallback = null)
        {
            if (string.IsNullOrEmpty(dbPath))
                throw new Exception("Invalid path");

            DatabaseHandler.errorCallback = errorCallback;
            DatabaseHandler.dbPath = dbPath;

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                connection = new SQLiteConnection(GetConnectionString());
            }

            connection ??= new SQLiteConnection(GetConnectionString());
            await GenerateTables();
            await HandleMigrations();
        }

        private static async Task GenerateTables()
        {
            // cannot add or modify existing columns. way too advanced for this

            Type[] tables = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Database_Table))).ToArray();

            await connection!.OpenAsync();

            foreach (Type tableType in tables)
            {
                Database_Table? table = Activator.CreateInstance(tableType) as Database_Table;

                using (SQLiteCommand command = new SQLiteCommand(table!.GenerateCreateCommand(), connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            await connection.CloseAsync();
        }

        private static async Task HandleMigrations()
        {
            Type[] migrations = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Database_MigrationBase))).ToArray();

            long? lastMigration = null;
            string? id = (await GetItem<dbo_Config>(QueryBuilder.SQLEquals(nameof(dbo_Config.key), Database_MigrationBase.CONFIG_MIGRATIONID)))?.value ?? null;

            if (!string.IsNullOrEmpty(id))
            {
                lastMigration = long.Parse(id);
            }

            Database_MigrationBase[] migrationsToApply = migrations.Select(x => (Database_MigrationBase)Activator.CreateInstance(x)!)
                .Where(x => x.migrationId > (lastMigration ?? 0))
                .OrderBy(x => x.migrationId).ToArray();

            if (lastMigration.HasValue)
            {
                lastMigration = null;

                await connection!.OpenAsync();

                foreach (Database_MigrationBase migration in migrationsToApply)
                {
                    lastMigration = migration.migrationId;

                    using (SQLiteCommand command = new SQLiteCommand(migration.Up(), connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await connection.CloseAsync();
            }
            else
            {
                // migrations in this context are only to update existing database TABLES,
                // as migrations are only for amending tables there is no need to do migration on a database that is has just been created

                lastMigration = migrationsToApply[migrations.Length - 1].migrationId;
            }

            if (lastMigration.HasValue)
            {
                await DeleteFromTable<dbo_Config>(QueryBuilder.SQLEquals(nameof(dbo_Config.key), Database_MigrationBase.CONFIG_MIGRATIONID));
                await InsertIntoTable(new dbo_Config() { key = Database_MigrationBase.CONFIG_MIGRATIONID, value = lastMigration.Value.ToString() });
            }
        }


        public static async Task InsertIntoTable(Database_Table value)
        {
            await TryExecute(value.GenerateInsertCommand());
        }

        public static async Task DeleteFromTable<T>(QueryBuilder.InternalAccessor queryBuilder) where T : Database_Table
        {
            await TryExecute($"DELETE FROM {GetTableNameFromGeneric<T>()} {queryBuilder?.BuildWhereClause() ?? ""}");
        }

        public static async Task UpdateTableEntry<T>(T entry, QueryBuilder.InternalAccessor queryBuilder) where T : Database_Table
        {
            string updateSQL = entry.GenerateUpdateCommand();
            await TryExecute($"{updateSQL} {queryBuilder?.BuildWhereClause() ?? ""}");
        }


        private static async Task TryExecute(string sql)
        {
            try
            {
                connection!.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                if (errorCallback != null) await errorCallback.Invoke(e);
            }
            finally
            {
                connection!.Close();
            }
        }


        public static async Task<bool> Exists<T>(QueryBuilder.InternalAccessor? queryBuilder = null) where T : Database_Table
        {
            try
            {
                bool exists = false;
                connection!.Open();

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

                connection!.Close();
                return exists;
            }
            catch (Exception e)
            {
                if (errorCallback != null) await errorCallback.Invoke(e);
                return false;
            }
            finally
            {
                connection!.Close();
            }
        }


        public static async Task<T?> GetItem<T>(QueryBuilder.InternalAccessor? queryBuilder = null) where T : Database_Table
            => (await GetItems<T>(queryBuilder, 1)).FirstOrDefault();

        public static async Task<T[]> GetItems<T>(QueryBuilder.InternalAccessor? queryBuilder = null, int? limit = null) where T : Database_Table
        {
            try
            {
                List<T> val = new List<T>(limit ?? 10);
                await connection!.OpenAsync();

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

                await connection.CloseAsync();
                return val.ToArray();
            }
            catch (Exception e)
            {
                await connection!.CloseAsync();
                if (errorCallback != null) await errorCallback.Invoke(e);
                return Array.Empty<T>();
            }
        }

        private static string GetTableNameFromGeneric<T>() where T : Database_Table
        {
            T table = (T)Activator.CreateInstance(typeof(T))!;
            return table.tableName;
        }


        public static async Task AddOrUpdate<T>(T change, QueryBuilder.InternalAccessor query) where T : Database_Table
        {
            if (await Exists<T>(query))
            {
                await UpdateTableEntry(change, query);
            }
            else
            {
                await InsertIntoTable(change);
            }
        }

        public static async Task AddOrUpdate<T>(T[] changes, Func<T, QueryBuilder.InternalAccessor> match) where T : Database_Table
        {
            foreach (T change in changes)
            {
                await AddOrUpdate(change, match(change));
            }
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

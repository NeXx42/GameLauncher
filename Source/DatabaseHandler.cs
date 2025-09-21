using GameLibary.Source.Database;
using GameLibary.Source.Database.Migrations;
using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;

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
            string? id = GetItems<dbo_Config>(new QueryBuilder().SearchEquals(nameof(dbo_Config.key), MigrationBase.CONFIG_MIGRATIONID)).FirstOrDefault()?.value ?? null;

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
                await DeleteFromTable<dbo_Config>(new QueryBuilder().SearchEquals(nameof(dbo_Config.key), MigrationBase.CONFIG_MIGRATIONID));
                await InsertIntoTable(new dbo_Config() { key = MigrationBase.CONFIG_MIGRATIONID, value = lastMigration.Value.ToString() });
            }
        }


        public static async Task InsertIntoTable(DatabaseTable value)
        {
            await TryExecute(value.GenerateInsertCommand());
        }

        public static async Task DeleteFromTable<T>(QueryBuilder queryBuilder) where T: DatabaseTable
        {
            await TryExecute($"DELETE FROM {GetTableNameFromGeneric<T>()} {queryBuilder?.BuildWhereClause() ?? ""}");
        }

        public static async Task UpdateTableEntry<T>(T entry, QueryBuilder queryBuilder) where T : DatabaseTable
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


        public static T[] GetItems<T>(QueryBuilder? queryBuilder = null) where T : DatabaseTable
        {
            try
            {
                List<T> val = new List<T>();
                connection.Open();

                using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM {GetTableNameFromGeneric<T>()} {queryBuilder?.BuildWhereClause() ?? ""}", connection))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T res = (T)Activator.CreateInstance(typeof(T));
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
                return null;
            }
        }

        private static string GetTableNameFromGeneric<T>() where T : DatabaseTable
        {
            T table = (T)Activator.CreateInstance(typeof(T));
            return table.tableName;
        }



        public class QueryBuilder
        {
            private string searchClause = "";

            private void PrepSearch()
            {
                if (!string.IsNullOrEmpty(searchClause))
                    searchClause += " AND";
            }

            public QueryBuilder SearchEquals(string column, string value)
            {
                PrepSearch();

                searchClause += $" {column} = '{value}'";
                return this;
            }

            public QueryBuilder SearchEquals(string column, int value)
            {
                PrepSearch();

                searchClause += $" {column} = {value}";
                return this;
            }

            public QueryBuilder SearchIn(string column, params int[] values)
            {
                PrepSearch();

                searchClause += $" {column} in ( ";

                for(int i = 0; i < values.Length; i++)
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

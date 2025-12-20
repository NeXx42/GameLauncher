using System.Collections.Concurrent;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Interfaces;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic
{
    public static class LibraryHandler
    {
        public enum OrderType
        {
            Id,
            Name,
            LastPlayed,
        }

        private static Action<int> globalGameChangeCallback;

        //private static GameDto[]? games;
        private static dbo_Tag[]? tags;

        private static Dictionary<int, GameDto> activeGameList = new Dictionary<int, GameDto>();

        public static bool getAreTagsDirty
        {
            get
            {
                if (m_AreTagsDirty)
                {
                    m_AreTagsDirty = false;
                    return true;
                }

                return false;
            }
        }
        private static bool m_AreTagsDirty;


        public static async Task Setup()
        {
            await RedetectGames();
            await FindTags();
        }


        public static async Task RedetectGames()
        {
            //await RefilterGames(null, null, OrderType.Id, true);

            //gameFilterList = null;
            //await FindGames_Internal();
        }

        //private static async Task FindGames_Internal()
        //{
        //    dbo_Game[] latestGames = await DatabaseHandler.GetItems<dbo_Game>();
        //    games = latestGames.Select(x => new GameDto(x)).ToArray();
        //
        //    foreach (GameDto game in games)
        //    {
        //        await game.LoadAll();
        //    }
        //}

        private static async Task FindTags()
        {
            tags = await DatabaseHandler.GetItems<dbo_Tag>();
        }


        public static void RegisterOnGlobalGameChange(Func<int, Task> callback) => globalGameChangeCallback += async (x) => await callback(x);
        public static void RegisterOnGlobalGameChange(Action<int> callback) => globalGameChangeCallback += callback;

        public static void InvokeGlobalGameChange(int gameId) => globalGameChangeCallback?.Invoke(gameId);


        public static async Task ImportGames(List<FileManager.FolderEntry> availableImports)
        {
            dbo_Libraries? chosenLibrary = await DatabaseHandler.GetItem<dbo_Libraries>();

            if (chosenLibrary == null)
            {
                throw new Exception("No library to import into");
            }

            bool useGuidFolderNames = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Import_GUIDFolderNames, true);

            for (int i = availableImports.Count - 1; i >= 0; i--)
            {
                FileManager.FolderEntry folder = availableImports[i];

                if (string.IsNullOrEmpty(folder.selectedBinary))
                    continue;

                string absoluteFolderPath = Path.GetDirectoryName(folder.selectedBinary);
                string gameFolderName = CorrectGameName(Path.GetFileName(absoluteFolderPath));

                dbo_Game newGame = new dbo_Game
                {
                    gameName = gameFolderName,
                    gameFolder = useGuidFolderNames ? Guid.NewGuid().ToString() : gameFolderName,
                    executablePath = Path.GetFileName(folder.selectedBinary),
                    libaryId = chosenLibrary.libaryId
                };

                try
                {
                    await DatabaseHandler.InsertIntoTable(newGame);
                    await FileManager.MoveGameToItsLibrary(newGame, absoluteFolderPath, chosenLibrary.rootPath);

                    availableImports.RemoveAt(i);
                }
                catch
                {

                }
            }

            await RedetectGames();

            string TryFindBestExecutable(string[] possible)
            {
                string? bestPossible = possible.Where(x =>
                {
                    string name = Path.GetFileName(x).ToLower();
                    return !name.Contains("crash", StringComparison.InvariantCulture)
                            && !name.Contains("crash", StringComparison.InvariantCulture);
                }).FirstOrDefault();

                return bestPossible ?? possible.FirstOrDefault() ?? "";
            }

            string CorrectGameName(string existing)
            {
                return existing.Replace("'", "");
            }
        }

        public static GameDto? TryGetCachedGame(int gameId) => activeGameList[gameId];

        public static async Task<int[]> GetGameList(GameFilterRequest filterRequest)
        {
            dbo_Game[] games = await DatabaseHandler.GetItems<dbo_Game>(filterRequest.ConstructSQL(), 10);

            foreach (dbo_Game game in games)
            {
                if (activeGameList.TryGetValue(game.id, out GameDto? gameObject))
                {
                    // may as well update the entry with the latest db version
                    await gameObject!.LoadGame(game);
                }
                else
                {
                    gameObject = new GameDto(game);
                    await gameObject.LoadAll();

                    activeGameList[game.id] = gameObject;
                }
            }

            return games.Select(x => x.id).ToArray();
        }


        //public static GameDto? GetGameFromId(int id) => games!.FirstOrDefault(x => x.getGameId == id);
        //
        //
        //public static void RefilterGames(HashSet<int> tagFilter, string? textFilter, OrderType orderType, bool isAsc)
        //{
        //    gameFilterList = games!
        //        .Filter_Tags(tagFilter)
        //        .Filter_Text(textFilter)
        //        .Filter_OrderType(orderType)
        //        .Filter_Direction(isAsc)
        //        .Select(x => x.getGameId)
        //        .ToArray();
        //}
        //
        //public static int GetFilteredGameCount() => gameFilterList?.Length ?? games!.Length;
        //
        //public static int[] GetDrawList(int offset, int take)
        //{
        //    if (gameFilterList == null)
        //        return games?.Skip(offset).Take(take).Select(x => x.getGameId).ToArray() ?? [];
        //
        //    return gameFilterList.Skip(offset).Take(take).Select(x => x).ToArray();
        //}

        public static async Task<int[]> GetAllTags()
        {
            if (m_AreTagsDirty)
            {
                await FindTags();
            }

            return tags!.Select(x => x.TagId).ToArray();
        }

        public static dbo_Tag? GetTagById(int id)
        {
            return tags!.FirstOrDefault(x => x.TagId == id);
        }

        public static void MarkTagsAsDirty() => m_AreTagsDirty = true;

        public static async Task<Exception?> DeleteGame(GameDto game)
        {
            try
            {
                Exception? fileDeletionFail = game.DeleteFiles();

                if (fileDeletionFail != null)
                {
                    return null;
                }
                //if (fileDeletionFail != null &&
                //    MessageBox.Show($"The folder deletion failed, Do you want to still remove the record?\n\n{fileDeletionFail.Message}", "Folder Delete Failed", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                //    return null;

                await DatabaseHandler.DeleteFromTable<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), game.getGameId));
                await DatabaseHandler.DeleteFromTable<dbo_Game>(QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.getGameId));

                await RedetectGames();

                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }


        public struct GameFilterRequest
        {
            public string? nameFilter;
            public HashSet<int>? tagList;

            public OrderType orderType;
            public bool orderDirection;

            public int page;
            public int contentPerPage;

            public string ConstructSQL()
            {
                dbo_Game _temp = new dbo_Game() { gameFolder = "", gameName = "", libaryId = 0 };
                dbo_GameTag _tempTag = new dbo_GameTag();

                StringBuilder sql = new StringBuilder($"SELECT g.* FROM {_temp.tableName} g ");

                List<string> joinClause = new List<string>();
                List<string> whereClause = new List<string>();
                List<string> groupClause = new List<string>();
                List<string> havingClause = new List<string>();

                if (!string.IsNullOrEmpty(nameFilter))
                {
                    whereClause.Add($"{nameof(dbo_Game.gameName)} like '{nameFilter}%'");
                }

                if (tagList?.Count > 0)
                {
                    joinClause.Add($"JOIN {_tempTag.tableName} gt ON gt.{nameof(dbo_GameTag.GameId)} = {nameof(dbo_Game.id)}");
                    whereClause.Add($"gt.{nameof(dbo_GameTag.TagId)} in ({string.Join(",", tagList)})");

                    groupClause.Add($"g.{nameof(dbo_Game.id)}");
                    havingClause.Add($"COUNT(DISTINCT gt.{nameof(dbo_GameTag.TagId)}) = {tagList.Count}");
                }

                if (joinClause.Count > 0)
                {
                    sql.Append(string.Join(" ", joinClause));
                }

                if (whereClause.Count > 0)
                {
                    sql.Append(" WHERE ");
                    sql.Append(string.Join(" AND ", whereClause));
                }

                if (groupClause.Count > 0)
                {
                    sql.Append(" GROUP BY ");
                    sql.Append(string.Join(" AND ", groupClause));
                }

                if (havingClause.Count > 0)
                {
                    sql.Append(" HAVING ");
                    sql.Append(string.Join(" AND ", havingClause));
                }

                sql.Append(CreateOrderBy());
                sql.Append(CreateLimit());

                string rawSql = sql.ToString();
                return rawSql;
            }

            private StringBuilder CreateOrderBy()
            {
                StringBuilder sql = new StringBuilder(" ORDER BY ");
                switch (orderType)
                {
                    case OrderType.Id: sql.Append($"g.{nameof(dbo_Game.id)}"); break;
                    case OrderType.Name: sql.Append($"g.{nameof(dbo_Game.gameName)}"); break;
                    case OrderType.LastPlayed: sql.Append($"g.{nameof(dbo_Game.lastPlayed)}"); break;
                }

                sql.Append(orderDirection ? " ASC" : " DESC");
                return sql;
            }

            private string CreateLimit()
            {
                int skip = contentPerPage * page;
                return $" LIMIT {contentPerPage} OFFSET {skip};";
            }
        }
    }
}

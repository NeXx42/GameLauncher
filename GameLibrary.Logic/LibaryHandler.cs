using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
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

        public static Action? onGameDeletion;
        public static Action<int>? onGameDetailsUpdate;

        //private static GameDto[]? games;
        private static dbo_Tag[]? tags;

        private static int filteredGameCount;
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
            await FindTags();
        }

        private static async Task FindTags()
        {
            tags = await Database_Manager.GetItems<dbo_Tag>();
        }

        public static async Task ImportGames(List<FileManager.IImportEntry> availableImports)
        {
            dbo_Libraries? chosenLibrary = await Database_Manager.GetItem<dbo_Libraries>();

            if (chosenLibrary == null)
            {
                throw new Exception("No library to import into");
            }

            bool useGuidFolderNames = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Import_GUIDFolderNames, true);

            for (int i = availableImports.Count - 1; i >= 0; i--)
            {
                FileManager.IImportEntry folder = availableImports[i];

                if (string.IsNullOrEmpty(folder.getBinaryPath))
                    continue;

                string gameName = folder.getPotentialName;

                dbo_Game newGame = new dbo_Game
                {
                    gameName = gameName,
                    gameFolder = useGuidFolderNames ? Guid.NewGuid().ToString() : gameName,
                    executablePath = Path.GetFileName(folder.getBinaryPath),
                    libaryId = chosenLibrary.libaryId
                };

                try
                {
                    await Database_Manager.InsertItem(newGame);
                    await FileManager.MoveGameToItsLibrary(newGame, folder.getBinaryPath, chosenLibrary.rootPath, string.IsNullOrEmpty(folder.getBinaryFolder));

                    availableImports.RemoveAt(i);
                }
                catch
                {

                }
            }
        }

        public static int GetMaxPages(int limit) => (int)Math.Ceiling(filteredGameCount / (float)limit) - 1;
        public static GameDto? TryGetCachedGame(int gameId) => activeGameList[gameId];

        public static async Task<int[]> GetGameList(GameFilterRequest filterRequest)
        {
            (dbo_Game[] games, filteredGameCount) = await Database_Manager.GetItemsWithCount<dbo_Game>(filterRequest.ConstructSQL());

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

        public static async Task DeleteGame(GameDto game)
        {
            try
            {
                await FileManager.DeleteGameFiles(game);
            }
            catch (Exception e)
            {
                string paragraph = $"Failed to delete games files!\n\n{e.Message}\n\nDo you want to delete the record anyway?";

                if (!await DependencyManager.uiLinker!.OpenYesNoModal("Delete record?", paragraph))
                    return;
            }

            await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), game.getGameId));
            await Database_Manager.Delete<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), game.getGameId));

            activeGameList.Remove(game.getGameId);
            onGameDeletion?.Invoke();
        }


        public static async Task GenerateLibrary(string path)
        {
            await Database_Manager.InsertItem(new dbo_Libraries()
            {
                rootPath = path
            });
        }

        public static async Task CreateTag(string tagName)
        {
            await Database_Manager.InsertItem(new dbo_Tag()
            {
                TagName = tagName,
            });
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
                StringBuilder sql = new StringBuilder($"SELECT g.*, count(*) OVER() as total_count FROM {dbo_Game.tableName} g ");

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
                    joinClause.Add($"JOIN {dbo_GameTag.tableName} gt ON gt.{nameof(dbo_GameTag.GameId)} = {nameof(dbo_Game.id)}");
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

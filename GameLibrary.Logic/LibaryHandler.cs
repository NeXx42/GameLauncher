using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
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

        private static GameDto[]? games;
        private static dbo_Tag[]? tags;

        private static int[]? gameFilterList;

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
            gameFilterList = null;
            await FindGames_Internal();
        }

        private static async Task FindGames_Internal()
        {
            dbo_Game[] latestGames = await DatabaseHandler.GetItems<dbo_Game>();
            games = latestGames.Select(x => new GameDto(x)).ToArray();

            foreach (GameDto game in games)
            {
                await game.LoadAll();
            }
        }

        private static async Task FindTags()
        {
            tags = await DatabaseHandler.GetItems<dbo_Tag>();
        }





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





        public static GameDto? GetGameFromId(int id) => games!.FirstOrDefault(x => x.getGameId == id);


        public static void RefilterGames(HashSet<int> tagFilter, OrderType orderType, bool isAsc)
        {
            if ((tagFilter?.Count ?? 0) == 0)
            {
                gameFilterList = OrderList(FilterList(games!.Select(x => x.getGameId))).ToArray();
                return;
            }

            gameFilterList = OrderList(FilterList(games!.Where(x => x.IsInFilter(ref tagFilter)).Select(x => x.getGameId))).ToArray();

            IEnumerable<int> OrderList(IEnumerable<int> inp)
                => isAsc ? inp : inp.Reverse();

            IEnumerable<int> FilterList(IEnumerable<int> inp)
            {
                switch (orderType)
                {
                    case OrderType.Id: return inp.OrderBy(x => x);
                    case OrderType.Name: return inp.OrderBy(x => GetGameFromId(x)!.getGame.gameName);
                    case OrderType.LastPlayed: return inp.OrderBy(x => GetGameFromId(x)!.getGame.lastPlayed);
                }

                return inp;
            }
        }

        public static int GetFilteredGameCount() => gameFilterList?.Length ?? games!.Length;

        public static int[] GetDrawList(int offset, int take)
        {
            if (gameFilterList == null)
                return games?.Skip(offset).Take(take).Select(x => x.getGameId).ToArray() ?? [];

            return gameFilterList.Skip(offset).Take(take).Select(x => x).ToArray();
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
    }
}

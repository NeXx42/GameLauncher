using GameLibary.Source.Database.Tables;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GameLibary.Source
{
    public static class LibaryHandler
    {
        public enum OrderType
        {
            Id,
            Name,
            LastPlayed,
        }

        private static bool isSetup = false;

        private static dbo_Game[]? games;
        private static dbo_Tag[]? tags;

        private static int[]? gameFilterList;

        public static bool isCrawling { private set; get; }

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

        private static ConcurrentQueue<(int gameId, Action<int, BitmapImage?> onFetch)> queuedImageFetch = new ConcurrentQueue<(int gameId, Action<int, BitmapImage?> onFetch)>();
        private static Thread? imageFetchThread;


        public static Action<int, BitmapImage?> onGlobalImageSet = null!;


        public static async Task Setup()
        {
            if (isSetup)
                return;

            isSetup = true;

            imageFetchThread = new Thread(GameImageFetcher);
            imageFetchThread.Name = "Image Thread";
            imageFetchThread.Start();

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
            games = await DatabaseHandler.GetItems<dbo_Game>();
            games ??= Array.Empty<dbo_Game>();
        }

        private static async Task FindTags()
        {
            tags = await DatabaseHandler.GetItems<dbo_Tag>();
        }




        public static void GetGameImage(dbo_Game game, Action<int, BitmapImage?> onFetch)
        {
            if (game.GetCachedImage() != null)
            {
                onFetch?.Invoke(game.id, game.GetCachedImage());
                return;
            }

            queuedImageFetch.Enqueue((game.id, onFetch));
        }

        private static async void GameImageFetcher()
        {
            while (true)
            {
                await Task.Delay(10);

                if (queuedImageFetch.Count == 0)
                    continue;

                if (!queuedImageFetch.TryDequeue(out (int gameId, Action<int, BitmapImage?> onFetch) a))
                    continue;

                dbo_Game game = GetGameFromId(a.gameId)!;

                if (game != null)
                {
                    string path = await game.GetIconLocation();

                    if (File.Exists(path))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(path);
                        //bitmap.DecodePixelWidth = 200;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        game.SetCachedIcon(bitmap);
                        Application.Current.Dispatcher.Invoke(() => a.onFetch?.Invoke(a.gameId, game.GetCachedImage()));
                    }
                }
            }
        }





        public static dbo_Game? GetGameFromId(int id) => games!.FirstOrDefault(x => x.id == id);


        public static async Task RefilterGames(HashSet<int> tagFilter, OrderType orderType, bool isAsc)
        {
            if ((tagFilter?.Count ?? 0) == 0)
            {
                gameFilterList = OrderList(FilterList(games!.Select(x => x.id))).ToArray();
                return;
            }

            dbo_GameTag[] gameTags = await DatabaseHandler.GetItems<dbo_GameTag>(QueryBuilder.In(nameof(dbo_GameTag.TagId), tagFilter!.ToArray()));
            gameFilterList = OrderList(FilterList(gameTags.GroupBy(x => x.GameId).Where(x => x.Count() == tagFilter!.Count).Select(x => x.Key))).ToArray();

            IEnumerable<int> OrderList(IEnumerable<int> inp)
                => isAsc ? inp : inp.Reverse();

            IEnumerable<int> FilterList(IEnumerable<int> inp)
            {
                switch (orderType)
                {
                    case OrderType.Id: return inp.OrderBy(x => x);
                    case OrderType.Name: return inp.OrderBy(x => GetGameFromId(x)!.gameName);
                    case OrderType.LastPlayed: return inp.OrderBy(x => GetGameFromId(x)!.lastPlayed);
                }

                return inp;
            }
        }

        public static void OrderFilterList()
        {
            gameFilterList = gameFilterList!.OrderByDescending(x => GetGameFromId(x)!.gameName).ToArray();
        }

        public static int GetFilteredGameCount() => gameFilterList!.Length;

        public static async Task<int[]> GetDrawList(int offset, int take)
        {
            if (gameFilterList == null)
                await RefilterGames(null!, OrderType.Name, true);

            List<int> res = new List<int>();

            for (int i = offset; i < MathF.Min(offset + take, gameFilterList!.Length); i++)
            {
                res.Add(gameFilterList[i]);
            }

            return res.ToArray();
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

        public static async Task<int[]> GetGameTags(int gameId)
        {
            return (await DatabaseHandler.GetItems<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), gameId))).Select(x => x.TagId).ToArray();
        }


        public static async void RemoveTagFromGame(int gameId, int tagId)
        {
            await DatabaseHandler.DeleteFromTable<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), gameId).SQLEquals(nameof(dbo_GameTag.TagId), tagId));
        }

        public static async void AddTagToGame(int gameId, int tagId)
        {
            await DatabaseHandler.InsertIntoTable(new dbo_GameTag() { GameId = gameId, TagId = tagId });
        }

        public static void MarkTagsAsDirty() => m_AreTagsDirty = true;


        public static async Task UpdateGameIcon(int gameId)
        {
            if (FileManager.GetTempScreenshot(out string path))
            {
                string screenshotName = await FileManager.PromoteTempFile(gameId, path);
                await UpdateGameIcon(gameId, screenshotName);
            }
        }

        public static async Task UpdateGameIcon(int gameId, string path)
        {
            dbo_Game? game = GetGameFromId(gameId);
            string requiresCleanup = await game!.GetIconLocation();

            if (game != null)
            {
                game.iconPath = path;
                game.SetCachedIcon(null);

                await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));

                GetGameImage(game, onGlobalImageSet);
            }

            if (File.Exists(requiresCleanup))
            {
                try
                {
                    File.Delete(requiresCleanup);
                }
                catch { }
            }
        }

        public static async void UpdateGameEmulationStatus(int gameId, bool to)
        {
            dbo_Game? game = GetGameFromId(gameId);

            if (game != null)
            {
                game.useEmulator = to;
                await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
            }
        }

        public static async Task ChangeBinaryLocation(int gameId, string? path)
        {
            dbo_Game? game = GetGameFromId(gameId);

            if (game != null)
            {
                string existing = game.executablePath ?? "";
                game.executablePath = path;

                if (!File.Exists(await game.GetExecutableLocation()))
                {
                    game.executablePath = existing;
                    return;
                }

                await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
            }
        }

        public static async Task<Exception?> DeleteGame(dbo_Game game)
        {
            try
            {
                Exception? fileDeletionFail = await FileManager.DeleteGame(game);

                if (fileDeletionFail != null &&
                    MessageBox.Show($"The folder deletion failed, Do you want to still remove the record?\n\n{fileDeletionFail.Message}", "Folder Delete Failed", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                    return null;

                await DatabaseHandler.DeleteFromTable<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), game.id));
                await DatabaseHandler.DeleteFromTable<dbo_Game>(QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));

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

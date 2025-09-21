using GameLibary.Source.Database.Tables;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GameLibary.Source
{
    public static class LibaryHandler
    {
        public enum OrderType
        {
            [Description("ID")]
            IdAsc,

            [Description("ID Descending")]
            IdDesc,

            [Description("Name")]
            NameAsc,

            [Description("Name Descending")]
            NameDesc,

            [Description("Last Played")]
            LastPlayedAsc,

            [Description("Last Played Descending")]
            LastPlayedDesc,
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
            FindTags();
        }


        public static async Task RedetectGames()
        {
            gameFilterList = null;
            await FindGames_Internal();
        }

        private static async Task FindGames_Internal()
        {
            games = DatabaseHandler.GetItems<dbo_Game>();
            games ??= Array.Empty<dbo_Game>();

            foreach (dbo_Game existingGame in games)
            {
                (bool isInvalid, bool wasMigrated) = await FileManager.TryMigrate(existingGame);

                if (isInvalid)
                {
                    continue;
                }

                if (wasMigrated)
                {
                    await DatabaseHandler.UpdateTableEntry(existingGame, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), existingGame.id));
                }
            }
        }

        private static void FindTags()
        {
            tags = DatabaseHandler.GetItems<dbo_Tag>();
        }




        public static void GetGameImage(dbo_Game game, Action<int, BitmapImage?> onFetch)
        {
            if(game.GetCachedImage() != null)
            {
                onFetch?.Invoke(game.id, game.GetCachedImage());
                return;
            }

            queuedImageFetch.Enqueue((game.id, onFetch));
        }

        private static async void GameImageFetcher()
        {
            while(true)
            {
                await Task.Delay(10);

                if (queuedImageFetch.Count == 0)
                    continue;

                if (!queuedImageFetch.TryDequeue(out (int gameId, Action<int, BitmapImage?> onFetch) a))
                    continue;

                dbo_Game game = GetGameFromId(a.gameId)!;

                if(game != null)
                {
                    if (File.Exists(game.GetRealIconPath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(game.GetRealIconPath);
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


        public static void RefilterGames(HashSet<int> tagFilter, OrderType orderType)
        {
            if((tagFilter?.Count ?? 0) == 0)
            {
                gameFilterList = FilterList(games!.Select(x => x.id));
                return;
            }

            dbo_GameTag[] gameTags = DatabaseHandler.GetItems<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchIn(nameof(dbo_GameTag.TagId), tagFilter!.ToArray()));
            gameFilterList = FilterList(gameTags.GroupBy(x => x.GameId).Where(x => x.Count() == tagFilter.Count).Select(x => x.Key));

            int[] FilterList(IEnumerable<int> inp)
            {
                switch (orderType)
                {
                    case OrderType.IdAsc: return inp.OrderBy(x => x).ToArray();
                    case OrderType.IdDesc: return inp.OrderByDescending(x => x).ToArray();

                    case OrderType.NameAsc: return inp.OrderBy(x => GetGameFromId(x)!.gameName).ToArray();
                    case OrderType.NameDesc: return inp.OrderByDescending(x => GetGameFromId(x)!.gameName).ToArray();

                    case OrderType.LastPlayedAsc: return inp.OrderBy(x => GetGameFromId(x)!.lastPlayed).ToArray();
                    case OrderType.LastPlayedDesc: return inp.OrderByDescending(x => GetGameFromId(x)!.lastPlayed).ToArray();
                }

                return inp.ToArray();
            }
        }

        public static void OrderFilterList()
        {
            gameFilterList = gameFilterList!.OrderByDescending(x => GetGameFromId(x)!.gameName).ToArray();
        }

        public static int GetFilteredGameCount() => gameFilterList!.Length;

        public static int[] GetDrawList(int offset, int take)
        {
            if (gameFilterList == null)
                RefilterGames(null!, OrderType.NameAsc);

            List<int> res = new List<int>();

            for (int i = offset; i < MathF.Min(offset + take, gameFilterList!.Length); i++)
            {
                res.Add(gameFilterList[i]);
            }

            return res.ToArray();
        }

        public static int[] GetAllTags()
        {
            if (m_AreTagsDirty)
            {
                FindTags();
            }

            return tags!.Select(x => x.TagId).ToArray();
        }

        public static dbo_Tag? GetTagById(int id)
        {
            return tags!.FirstOrDefault(x => x.TagId == id);
        }

        public static int[] GetGameTags(int gameId)
        {
            return DatabaseHandler.GetItems<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_GameTag.GameId), gameId)).Select(x => x.TagId).ToArray();
        }


        public static async void RemoveTagFromGame(int gameId, int tagId)
        {
            await DatabaseHandler.DeleteFromTable<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_GameTag.GameId), gameId).SearchEquals(nameof(dbo_GameTag.TagId), tagId));
        }

        public static async void AddTagToGame(int gameId, int tagId)
        {
            await DatabaseHandler.InsertIntoTable(new dbo_GameTag() { GameId = gameId, TagId = tagId });
        }

        public static void MarkTagsAsDirty() => m_AreTagsDirty = true;


        public static void UpdateGameIcon(int gameId)
        {
            if (FileManager.GetTempScreenshot(out string path))
            {
                string screenshotName = FileManager.PromoteTempFile(gameId, path);
                UpdateGameIcon(gameId, screenshotName);
            }
        }

        public static async void UpdateGameIcon(int gameId, string path)
        {
            dbo_Game? game = GetGameFromId(gameId);
            string requiresCleanup = game!.GetRealIconPath;

            if (game != null)
            {
                game.iconPath = path;
                game.SetCachedIcon(null);

                await DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));

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
                await DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));
            }
        }

        public static async void ChangeBinaryLocation(int gameId, string? path)
        {
            dbo_Game? game = GetGameFromId(gameId);

            if (game != null)
            {
                string existing = game.executablePath ?? "";
                game.executablePath = $"#{path}";

                if (!File.Exists(game.GetRealExecutionPath))
                {
                    game.executablePath = existing;
                    return;
                }

                await DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));
            }
        }

        public static async Task<Exception?> DeleteGame(dbo_Game game)
        {
            try
            {
                Exception? fileDeletionFail = FileManager.DeleteGame(game);

                if (fileDeletionFail != null && 
                    MessageBox.Show($"Continue with delete?\n\n{fileDeletionFail.Message}", "Skip Folder Cleanup?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return fileDeletionFail;

                await DatabaseHandler.DeleteFromTable<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_GameTag.GameId), game.id));
                await DatabaseHandler.DeleteFromTable<dbo_Game>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));

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

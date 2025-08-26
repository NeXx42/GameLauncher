using GameLibary.Source.Database.Tables;
using System.IO;
using System.Windows.Controls;

namespace GameLibary.Source
{
    public static class LibaryHandler
    {
        private static dbo_Game[] games;
        private static dbo_Tag[] tags;

        private static int[] gameFilterList;

        public static bool isCrawling { private set; get; }
        private static Thread crawlingThread;

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
            await FindGames();
            FindTags();
        }


        private static async Task FindGames()
        {
            gameFilterList = null;

            isCrawling = true;

            await FindGames_Internal();

            //crawlingThread = new Thread(FindGames_Internal);
            //crawlingThread.Start();
        }

        private static async Task FindGames_Internal()
        {
            var temp = CrawlGames();

            List<dbo_Game> newGames = new List<dbo_Game>();

            for (int i = 0; i < temp.Count; i++)
            {
                string exectuable = temp[i].exectuables.FirstOrDefault() ?? "";
                string dirName = Path.GetDirectoryName(temp[i].exectuables.FirstOrDefault());

                dbo_Game newGame = new dbo_Game
                {
                    gameName = Path.GetFileName(temp[i].path),
                    executablePath = exectuable
                };

                (bool isInvalid, bool wasMigrated) = await FileManager.TryMigrate(newGame);

                if (!isInvalid)
                    newGames.Add(newGame);
            }

            foreach (dbo_Game game in newGames)
            {
                await DatabaseHandler.InsertIntoTable(game);
            }

            games = DatabaseHandler.GetItems<dbo_Game>();

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






        public static dbo_Game? GetGameFromId(int id) => games.FirstOrDefault(x => x.id == id);


        public static void RefilterGames(HashSet<int> tagFilter)
        {
            if((tagFilter?.Count ?? 0) == 0)
            {
                gameFilterList = games.Select(x => x.id).ToArray();
                return;
            }

            dbo_GameTag[] gameTags = DatabaseHandler.GetItems<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchIn(nameof(dbo_GameTag.TagId), tagFilter.ToArray()));
            gameFilterList = gameTags.GroupBy(x => x.GameId).Where(x => x.Count() == tagFilter.Count).Select(x => x.Key).ToArray();
        }

        public static int GetFilteredGameCount() => gameFilterList.Length;

        public static int[] GetDrawList(int offset, int take)
        {
            if (gameFilterList == null)
                RefilterGames(null);

            List<int> res = new List<int>();

            for (int i = offset; i < MathF.Min(offset + take, gameFilterList.Length); i++)
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

            return tags.Select(x => x.TagId).ToArray();
        }

        public static dbo_Tag? GetTagById(int id)
        {
            return tags.FirstOrDefault(x => x.TagId == id);
        }

        public static int[] GetGameTags(int gameId)
        {
            return DatabaseHandler.GetItems<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_GameTag.GameId), gameId)).Select(x => x.TagId).ToArray();
        }


        public static void RemoveTagFromGame(int gameId, int tagId)
        {
            DatabaseHandler.DeleteFromTable<dbo_GameTag>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_GameTag.GameId), gameId).SearchEquals(nameof(dbo_GameTag.TagId), tagId));
        }

        public static void AddTagToGame(int gameId, int tagId)
        {
            DatabaseHandler.InsertIntoTable(new dbo_GameTag() { GameId = gameId, TagId = tagId });
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

        public static void UpdateGameIcon(int gameId, string path)
        {
            dbo_Game? game = GetGameFromId(gameId);
            string requiresCleanup = game.GetRealIconPath;

            if (game != null)
            {
                game.iconPath = path;
                DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));
            }

            MainWindow.window.DrawGames();

            if (File.Exists(requiresCleanup))
            {
                try
                {
                    File.Delete(requiresCleanup);
                }
                catch { }
            }
        }

        public static void UpdateGameEmulationStatus(int gameId, bool to)
        {
            dbo_Game? game = GetGameFromId(gameId);

            if (game != null)
            {
                game.useEmulator = to;
                DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));
            }
        }

        public static void ChangeBinaryLocation(int gameId, string? path)
        {
            dbo_Game? game = GetGameFromId(gameId);

            if (game != null)
            {
                string existing = game.executablePath;
                game.executablePath = $"#{path}";

                if (!File.Exists(game.GetRealExecutionPath))
                {
                    game.executablePath = existing;
                    return;
                }

                DatabaseHandler.UpdateTableEntry(game, new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Game.id), game.id));
            }
        }






        private static List<GameFolder> CrawlGames()
        {
            List<GameFolder> foundGameFolders = new List<GameFolder>();
            List<GameZip> foundZips = new List<GameZip>();

            Craw(MainWindow.GameRootLocation);

            foreach (GameZip zip in foundZips)
            {
                Button btn = new Button();
                btn.Content = Path.GetFileName(zip.path);

                //cont_zips.Children.Add(btn);
            }


            void Craw(string path)
            {
                if (string.Equals(path, FileManager.GetProcessGameLocation(), StringComparison.CurrentCultureIgnoreCase))
                    return;

                string[] allFiles = Directory.GetFiles(path);
                string[] binaries = allFiles.Where(x => x.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)).ToArray();

                if (binaries.Length > 0)
                {
                    foundGameFolders.Add(new GameFolder()
                    {
                        path = path,
                        exectuables = binaries
                    });
                    return;
                }

                string[] zips = allFiles.Where(IsZip).ToArray();

                if (zips.Length > 0)
                {
                    foreach (string zip in zips)
                    {
                        foundZips.Add(new GameZip()
                        {
                            path = zip
                        });
                    }
                }

                string[] subDirs = Directory.GetDirectories(path);

                foreach (string dir in subDirs)
                {
                    Craw(dir);
                }
            }

            return foundGameFolders;
        }

        private static bool IsZip(string path)
        {
            return path.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".rar", StringComparison.InvariantCultureIgnoreCase);
        }


        private struct GameFolder
        {
            public string path;
            public string[] exectuables;
        }

        private struct GameZip
        {
            public string path;
        }
    }
}

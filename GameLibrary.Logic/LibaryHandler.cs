using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic
{
    public static class LibraryHandler
    {
        public enum ExternalLibraryTypes
        {
            Steam
        }

        public static Action? onGameDeletion;
        public static Action<int>? onGameDetailsUpdate;

        //private static GameDto[]? games;
        private static dbo_Tag[]? tags;

        private static int filteredGameCount;
        private static Dictionary<int, GameDto> activeGameList = new Dictionary<int, GameDto>();
        private static Dictionary<int, LibraryDto> cachedLibraries = new Dictionary<int, LibraryDto>();

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
            await FindLibraries();
            await FindTags();
        }

        private static async Task FindLibraries()
        {
            dbo_Libraries[] libraries = await Database_Manager.GetItems<dbo_Libraries>();
            cachedLibraries = libraries.ToDictionary(x => x.libaryId, x => new LibraryDto(x));
        }


        private static async Task FindTags()
        {
            tags = await Database_Manager.GetItems<dbo_Tag>();
        }

        public static async Task ImportGames(List<FileManager.IImportEntry> availableImports, int? libraryId)
        {
            bool useGuidFolderNames = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Import_GUIDFolderNames, true);

            for (int i = availableImports.Count - 1; i >= 0; i--)
            {
                FileManager.IImportEntry folder = availableImports[i];

                if (string.IsNullOrEmpty(folder.getBinaryPath))
                    continue;

                string gameName = folder.getPotentialName;
                string absoluteFolder;

                if (folder is FileManager.ImportEntry_Binary binaryImport)
                {
                    absoluteFolder = FileManager.CreateEmptyGameFolder(binaryImport.binaryLocation);
                }
                else
                {
                    absoluteFolder = folder.getBinaryFolder!;
                }

                dbo_Game newGame = new dbo_Game
                {
                    gameName = gameName,
                    gameFolder = absoluteFolder,
                    executablePath = Path.GetFileName(folder.getBinaryPath),
                    libraryId = libraryId,
                    status = (int)GameDto.Status.Active
                };

                try
                {
                    if (libraryId.HasValue)
                    {
                        newGame.gameFolder = useGuidFolderNames ? Guid.NewGuid().ToString() : gameName;
                        dbo_Libraries library = (await Database_Manager.GetItem<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libaryId), libraryId.Value)))!;

                        await FileManager.MoveGameToItsLibrary(newGame, folder.getBinaryPath, library.rootPath);
                    }

                    await Database_Manager.InsertItem(newGame);
                    availableImports.RemoveAt(i);
                }
                catch
                {

                }
            }
        }

        public static int GetMaxPages(int limit) => (int)Math.Ceiling(filteredGameCount / (float)limit) - 1;


        public static bool TryGetCachedGame(int? gameId, out GameDto? game)
        {
            game = null;

            if (!gameId.HasValue)
                return false;

            return activeGameList.TryGetValue(gameId.Value, out game);
        }
        public static GameDto? TryGetCachedGame(int gameId) => activeGameList[gameId];

        public static async Task<int[]> GetGameList(GameFilterRequest filterRequest)
        {
            (dbo_Game[] games, filteredGameCount) = await Database_Manager.GetItemsWithCount<dbo_Game>(filterRequest.ConstructSQL());

            foreach (dbo_Game game in games)
            {
                if (!activeGameList.TryGetValue(game.id, out GameDto? gameObject))
                    await LoadGame(game);
            }

            return games.Select(x => x.id).ToArray();
        }

        private static async Task LoadGame(dbo_Game game)
        {
            GameDto? dto = null;
            dbo_GameTag[] gameTags = await Database_Manager.GetItems<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), game.id));

            if (game.libraryId.HasValue && cachedLibraries.TryGetValue(game.libraryId.Value, out LibraryDto? lib) && lib != null)
            {
                switch (lib.externalType)
                {
                    case LibraryDto.ExternalTypes.Steam:
                        dto = new GameDto_Steam(game, gameTags);
                        break;
                }
            }

            dto ??= new GameDto_Custom(game, gameTags);
            activeGameList[game.id] = dto;
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

        public static string? GetTagName(int id) => tags!.FirstOrDefault(x => x.TagId == id)?.TagName;

        public static void MarkTagsAsDirty() => m_AreTagsDirty = true;

        public static async Task DeleteGame(GameDto game, bool removeFiles)
        {
            if (removeFiles)
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
            }

            await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), game.gameId));
            await Database_Manager.Delete<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), game.gameId));

            activeGameList.Remove(game.gameId);
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

        public static string RouteLibrary(GameDto game)
        {
            if (!game.libraryId.HasValue)
                return game.folderPath;

            // want it to error if this doesn't exist
            return Path.Combine(cachedLibraries[game.libraryId.Value].root, game.folderPath);
        }

        public static LibraryDto[] GetLibraries() => cachedLibraries.Values.ToArray();
    }
}

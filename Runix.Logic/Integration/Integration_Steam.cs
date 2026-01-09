using System.Text.Json;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using ValveKeyValue;

namespace GameLibrary.Logic.Integration;

public static class Integration_Steam
{
    public static async Task SyncLibrary()
    {
        int libId = await GetOrCreateLibrary();

        (string, long)[]? installedApps = await FindMounts();

        if (installedApps == null)
            return;

        dbo_Game[]? savedSteamGames = await Database_Manager.GetItems<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.libraryId), libId));

        Dictionary<long, dbo_Game> existingGames = savedSteamGames.ToDictionary(x => long.Parse(x.executablePath!), x => x);
        savedSteamGames = null;

        List<dbo_Game> newGames = new List<dbo_Game>();

        using (HttpClient client = new HttpClient())
        {
            foreach ((string root, long gameId) in installedApps)
            {
                if (existingGames.ContainsKey(gameId))
                {
                    // maybe update?
                    existingGames.Remove(gameId);
                    continue;
                }

                HttpResponseMessage res = await client.GetAsync($"https://store.steampowered.com/api/appdetails?appids={gameId}");

                if (!res.IsSuccessStatusCode)
                    continue;

                Stream json = await res.Content.ReadAsStreamAsync();
                JsonElement doc = (await JsonDocument.ParseAsync(json)).RootElement.EnumerateObject().ElementAt(0).Value;

                dbo_Game? gameObj = await CreateNewGameObject(gameId, root, doc, libId);

                if (gameObj != null)
                    newGames.Add(gameObj);
            }
        }

        await LibraryManager.InsertGames(newGames.ToArray());
    }

    private static async Task<int> GetOrCreateLibrary()
    {
        dbo_Libraries? existingLib = await Database_Manager.GetItem<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libraryExternalType), (int)Library_ExternalProviders.Steam));

        if (existingLib == null)
        {
            existingLib = new dbo_Libraries()
            {
                rootPath = "steamlib",
                libraryExternalType = (int)Library_ExternalProviders.Steam,
            };

            await Database_Manager.InsertItem(existingLib);
            existingLib = await Database_Manager.GetItem<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libraryExternalType), (int)Library_ExternalProviders.Steam)); // need to refetch for auto id
        }

        return existingLib!.libaryId;
    }

    private static async Task<(string, long)[]?> FindMounts()
    {
        string libraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam/steamapps/libraryfolders.vdf");

        if (!File.Exists(Path.Combine(libraryPath)))
            return null;

        List<(string, long)> discoveredGames = new List<(string, long)>();

        using (FileStream stream = new FileStream(libraryPath, new FileStreamOptions()
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.ReadWrite
        }))
        {
            KVSerializer serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            KVDocument doc = serializer.Deserialize(stream);

            foreach (KVObject lib in doc.Children)
            {
                string root = lib.Children.First(x => x.Name.Equals("path")).Value.ToString()!;
                long[] games = lib.Children.FirstOrDefault(x => x.Name.Equals("apps"))?.Children.Select(x => long.Parse(x.Name.ToString()!)).ToArray() ?? [];

                foreach (long game in games)
                {
                    discoveredGames.Add((root, game));
                }
            }
        }

        return discoveredGames.ToArray();
    }

    private static async Task<dbo_Game?> CreateNewGameObject(long id, string root, JsonElement doc, int libraryId)
    {
        if (!doc.GetProperty("success").GetBoolean())
            return CreatePlaceholder();

        JsonElement data = doc.GetProperty("data");

        if (data.GetProperty("type").GetString() != "game")
            return CreatePlaceholder();


        string manifestPath = Path.Combine(root, "steamapps", $"appmanifest_{id}.acf");

        using (FileStream stream = new FileStream(manifestPath, new FileStreamOptions()
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.ReadWrite
        }))
        {
            KVSerializer serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            KVDocument fileDoc = serializer.Deserialize(stream);

            string folderName = Path.Combine(root, "steamapps", "common", fileDoc.Children.First(x => x.Name.Equals("installdir")).Value.ToString()!);

            return new dbo_Game()
            {
                gameName = data.GetProperty("name").GetString() ?? "INVALID",
                iconPath = data.GetProperty("header_image").GetString(),

                executablePath = id.ToString(),
                gameFolder = folderName,

                libraryId = libraryId,
                status = (int)Game_Status.Active
            };
        }

        dbo_Game CreatePlaceholder() => new dbo_Game()
        {
            executablePath = id.ToString(),
            gameFolder = root,

            gameName = "steam_placeholder",
            status = (int)Game_Status.Placeholder,
            libraryId = libraryId
        };
    }
}

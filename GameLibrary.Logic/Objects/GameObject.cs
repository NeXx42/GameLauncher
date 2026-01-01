using System.Diagnostics;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects;

public interface IGameDto
{
    public int getGameId { get; }

    public bool captureLogs { get; }
    public bool useRegionEmulation { get; }

    public string getAbsoluteFolderLocation { get; }
    public string getAbsoluteBinaryLocation { get; }
}

public class GameForge : IGameDto
{
    public required string path;

    public int getGameId => -1;
    public bool captureLogs => false;

    public bool useRegionEmulation => false;

    public string getAbsoluteFolderLocation => Path.GetDirectoryName(path)!;
    public string getAbsoluteBinaryLocation => path;
}

public class GameDto : IGameDto
{
    private int gameId;

    private dbo_Game? game;

    private HashSet<int>? tags;
    private dbo_Libraries? library;

    // loading

    public bool IsInFilter(ref HashSet<int> filters)
    {
        if (tags == null)
            return false;

        foreach (int filter in filters)
            if (!tags.Contains(filter))
                return false;

        return true;
    }

    public GameDto(dbo_Game game)
    {
        this.gameId = game.id;
        this.game = game;
    }

    public async Task LoadAll(bool reloadGame = true)
    {
        if (reloadGame)
            await LoadGame();

        await LoadTags();
        await LoadLibrary();
    }

    public async Task LoadGame(dbo_Game? dbGame = null)
    {
        if (dbGame != null)
        {
            game = dbGame;
            return;
        }

        game = await Database_Manager.GetItem<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), gameId));
    }

    public async Task UpdateGame()
    {
        await Database_Manager.Update(game!, SQLFilter.Equal(nameof(dbo_Game.id), getGameId));
        LibraryHandler.onGameDetailsUpdate?.Invoke(gameId);
    }

    public async Task LoadTags()
    {
        dbo_GameTag[] actualTags = await Database_Manager.GetItems<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId));
        tags = actualTags.Select(x => x.TagId).ToHashSet();
    }

    public async Task LoadLibrary()
    {
        library = await Database_Manager.GetItem<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libaryId), game!.libaryId));
    }



    // props

    public int getGameId => gameId;
    public bool captureLogs => game?.captureLogs ?? false;
    public bool useRegionEmulation => game?.useEmulator ?? false;

    public dbo_Game getGame => game!;
    public HashSet<int> getTags => tags!;

    public string getAbsoluteFolderLocation => Path.Combine(library!.rootPath, game!.gameFolder!);

    public string getAbsoluteIconPath => Path.Combine(getAbsoluteFolderLocation, game?.iconPath ?? "INVALID.INVALID");
    public string getAbsoluteBinaryLocation => Path.Combine(getAbsoluteFolderLocation, game?.executablePath ?? "INVALID.INVALID");


    // changing properties

    public async Task UpdateGameIcon(string path)
    {
        if (File.Exists(getAbsoluteIconPath))
        {
            try
            {
                File.Delete(getAbsoluteIconPath);
            }
            catch { }
        }

        game!.iconPath = path;

        await Database_Manager.Update(game, SQLFilter.Equal(nameof(dbo_Game.id), game.id), nameof(dbo_Game.iconPath));
        ImageManager.ClearCache(game.id);
    }

    public async Task UpdateGameEmulationStatus(bool to)
    {
        game!.useEmulator = to;
        await UpdateGame();
    }

    public async Task UpdateCaptureLogsStatus(bool to)
    {
        game!.captureLogs = to;
        await UpdateGame();
    }

    public async Task ChangeBinaryLocation(string? path)
    {
        string newAbsolutePath = Path.Combine(getAbsoluteFolderLocation, path!);

        if (!File.Exists(newAbsolutePath))
        {
            return;
        }

        game!.executablePath = path;
        await UpdateGame();
    }

    public async Task ChangeWineProfile(int? wineProfile)
    {
        game!.runnerId = wineProfile;
        await UpdateGame();
        //await LoadWineProfile();
    }

    public async Task UpdateGameName(string newName)
    {
        game!.gameName = newName;
        await UpdateGame();
    }


    public void BrowseToGame()
    {
        if (string.IsNullOrEmpty(getAbsoluteFolderLocation) || !Directory.Exists(getAbsoluteFolderLocation))
            return;

        if (ConfigHandler.isOnLinux)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = getAbsoluteFolderLocation,
                UseShellExecute = false
            });
        }
        else
        {
            Process.Start("explorer.exe", getAbsoluteFolderLocation);
        }
    }


    public string PromoteTempFile(string path)
    {
        string extension = Path.GetExtension(path);
        string newName = $"{Guid.NewGuid()}{extension}";

        File.Move(path, Path.Combine(getAbsoluteFolderLocation, newName));
        return newName;
    }

    public async Task ToggleTag(int tagId)
    {
        if (tags!.Contains(tagId))
        {
            tags.Remove(tagId);
            await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId).Equal(nameof(dbo_GameTag.TagId), tagId));
        }
        else
        {
            tags.Add(tagId);
            await Database_Manager.InsertItem(new dbo_GameTag() { GameId = gameId, TagId = tagId });
        }
    }


    // misc


    public (int? selected, string[] options) GetPossibleBinaries()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return (null, []);

        List<string> binaries = Directory.GetFiles(getAbsoluteFolderLocation).Where(RunnerManager.IsUniversallyAcceptedExecutableFormat).Select(x => Path.GetFileName(x)).ToList();
        return (binaries.IndexOf(game!.executablePath!), binaries.ToArray());
    }

    public async Task Launch()
    {
        await RunnerManager.RunGame(new RunnerManager.GameLaunchRequest()
        {
            gameId = getGameId,
            path = getAbsoluteBinaryLocation,
            runnerId = game?.runnerId ?? -1
        });
    }

    public async Task UpdateLastPlayed()
    {
        game!.lastPlayed = DateTime.UtcNow;
        await Database_Manager.AddOrUpdate(game, SQLFilter.Equal(nameof(dbo_Game.id), gameId), nameof(dbo_Game.lastPlayed));
    }
}

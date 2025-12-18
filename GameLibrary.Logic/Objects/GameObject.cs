using System.Diagnostics;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects;

public class GameDto
{
    private int gameId;

    private dbo_Game? game;

    private HashSet<int>? tags;
    private dbo_Libraries? library;

    private dbo_WineProfile? wineProfile;

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
        await LoadWineProfile();
    }

    public async Task LoadGame()
    {
        game = await DatabaseHandler.GetItem<dbo_Game>(QueryBuilder.SQLEquals(nameof(dbo_Game.id), gameId));
    }

    public async Task LoadTags()
    {
        dbo_GameTag[] actualTags = await DatabaseHandler.GetItems<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), gameId));
        tags = actualTags.Select(x => x.TagId).ToHashSet();
    }

    public async Task LoadLibrary()
    {
        library = await DatabaseHandler.GetItem<dbo_Libraries>(QueryBuilder.SQLEquals(nameof(dbo_Libraries.libaryId), game.libaryId));
    }

    public async Task LoadWineProfile()
    {
        if (game?.wineProfile.HasValue ?? false)
        {
            wineProfile = await DatabaseHandler.GetItem<dbo_WineProfile>(QueryBuilder.SQLEquals(nameof(dbo_WineProfile.id), game!.wineProfile!.Value));
            return;
        }

        wineProfile = null;
    }



    // props

    public int getGameId => gameId;

    public dbo_Game getGame => game!;
    public HashSet<int> getTags => tags!;

    public dbo_WineProfile? getWineProfile => wineProfile;

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

        await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
        ImageManager.ClearCache(game.id);
    }

    public async Task UpdateGameEmulationStatus(bool to)
    {
        game!.useEmulator = to;
        await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
    }

    public async Task ChangeBinaryLocation(string? path)
    {
        string newAbsolutePath = Path.Combine(getAbsoluteFolderLocation, path!);

        if (!File.Exists(newAbsolutePath))
        {
            return;
        }

        game!.executablePath = path;
        await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
    }

    public async Task ChangeWineProfile(int? wineProfile)
    {
        game!.wineProfile = wineProfile;
        await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), game.id));
    }

    public Exception? DeleteFiles()
    {
        // do retry and stuff here, need a global way of showing dialogs and the like

        return null;

        try
        {
            if (Directory.Exists(getAbsoluteFolderLocation))
                Directory.Delete(getAbsoluteFolderLocation, true);

            return null;
        }
        catch (Exception e)
        {
            return e;
        }
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
            await DatabaseHandler.DeleteFromTable<dbo_GameTag>(QueryBuilder.SQLEquals(nameof(dbo_GameTag.GameId), gameId).SQLEquals(nameof(dbo_GameTag.TagId), tagId));
        }
        else
        {
            tags.Add(tagId);
            await DatabaseHandler.InsertIntoTable(new dbo_GameTag() { GameId = gameId, TagId = tagId });
        }
    }


    // misc


    public (int? selected, string[] options) GetPossibleBinaries()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return (null, []);

        List<string> binaries = Directory.GetFiles(getAbsoluteFolderLocation).Where(FilterFile).Select(x => Path.GetFileName(x)).ToList();
        return (binaries.IndexOf(game!.executablePath!), binaries.ToArray());

        bool FilterFile(string dir)
        {
            return dir.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) ||
                dir.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public void Launch()
    {
        GameLauncher.LaunchGame(this);
    }

    public async Task UpdateLastPlayed()
    {
        game!.lastPlayed = DateTime.UtcNow;
        await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), gameId));
    }
}

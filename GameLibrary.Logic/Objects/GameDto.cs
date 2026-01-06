using System.Diagnostics;
using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Helpers;
using Logic.db;

namespace GameLibrary.Logic.Objects;

public abstract class GameDto
{
    public readonly int gameId;

    public string gameName { protected set; get; }
    public string folderPath { protected set; get; }

    public string? iconPath { protected set; get; }
    public string? binaryPath { protected set; get; }

    public int? libraryId { protected set; get; }
    public int? runnerId { protected set; get; }
    public Game_Status status { protected set; get; }

    public int? minsPlayed { protected set; get; }
    public DateTime? lastPlayed { protected set; get; }

    public HashSet<int> tags { protected set; get; }
    public ConfigProvider<Game_Config> config { protected set; get; }


    public virtual string getAbsoluteFolderLocation => Path.Combine(LibraryManager.GetLibraryRoute(this), folderPath);
    public virtual string getAbsoluteLogFile => $"{getAbsoluteBinaryLocation}.log";

    protected virtual string getAbsoluteIconPath => Path.Combine(getAbsoluteFolderLocation, iconPath ?? "");
    public virtual string getAbsoluteBinaryLocation => Path.Combine(getAbsoluteFolderLocation, binaryPath ?? "INVALID.INVALID");


    public bool IsInFilter(ref HashSet<int> filters)
    {
        if (tags == null)
            return false;

        foreach (int filter in filters)
            if (!tags.Contains(filter))
                return false;

        return true;
    }

    public GameDto(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config)
    {
        this.gameId = game.id;
        this.gameName = game.gameName;

        this.iconPath = game.iconPath;
        this.folderPath = game.gameFolder;
        this.binaryPath = game.executablePath;

        this.minsPlayed = game.minsPlayed;
        this.lastPlayed = game.lastPlayed;

        this.runnerId = game.runnerId;
        this.libraryId = game.libraryId;
        this.status = (Game_Status)game.status;

        this.tags = tags.Select(x => x.TagId).ToHashSet();
        this.config = new ConfigProvider<Game_Config>(config.Select(x => (x.configKey, x.configValue)), SaveConfig, DeleteConfig);
    }

    protected async Task UpdateDatabaseEntry(params string[] columns)
    {
        await Database_Manager.Update(new dbo_Game()
        {
            id = gameId,

            gameName = gameName,
            gameFolder = folderPath,

            iconPath = iconPath,
            executablePath = binaryPath,

            minsPlayed = minsPlayed,
            lastPlayed = lastPlayed,

            runnerId = runnerId,
            libraryId = libraryId,
            status = (int)status

        }, SQLFilter.Equal(nameof(dbo_Game.id), gameId), columns);

        LibraryManager.InvokeGameDetailsUpdate(gameId);
    }

    public async Task Delete()
    {
        await Database_Manager.Delete<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId));
        await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId));
        await Database_Manager.Delete<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), gameId));
    }

    // Config

    private async Task SaveConfig(string key, string val)
    {
        dbo_GameConfig conf = new dbo_GameConfig()
        {
            gameId = gameId,
            configKey = key,
            configValue = val
        };

        await Database_Manager.AddOrUpdate(conf, SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId).Equal(nameof(dbo_GameConfig.configKey), key));
    }

    private async Task DeleteConfig(string key)
    {
        await Database_Manager.Delete<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId).Equal(nameof(dbo_GameConfig.configKey), key.ToString()));
    }

    // updating properties

    public async Task UpdateGameName(string newName)
    {
        gameName = newName;
        await UpdateDatabaseEntry(nameof(dbo_Game.gameName));
    }

    public async Task UpdateGameIcon(string path)
    {
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(getAbsoluteIconPath))
        {
            try
            {
                File.Delete(getAbsoluteIconPath);
            }
            catch { }
        }

        iconPath = path;

        ImageManager.ClearCache(gameId);
        await UpdateDatabaseEntry(nameof(dbo_Game.iconPath));
    }

    // default behaviour 

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

        LibraryManager.onGameDetailsUpdate?.Invoke(gameId);
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

    public async Task UpdateLastPlayed()
    {
        lastPlayed = DateTime.UtcNow;
        await UpdateDatabaseEntry(nameof(dbo_Game.lastPlayed));
    }

    public async Task UpdatePlayTime(int extraMins)
    {
        minsPlayed ??= 0;
        minsPlayed += extraMins;

        await UpdateDatabaseEntry(nameof(dbo_Game.minsPlayed));
    }

    public string GetLastPlayedFormatted()
    {
        if (lastPlayed == null) return "Never";

        TimeSpan time = DateTime.UtcNow - lastPlayed.Value;

        if (time.TotalDays > 0) return Format(time.TotalDays, "day");
        if (time.TotalHours > 0) return Format(time.TotalHours, "hour");
        if (time.TotalMinutes > 0) return Format(time.TotalMinutes, "min");

        return "Just now";

        string Format(double time, string interval)
        {
            double rounded = Math.Ceiling(time);
            return $"{rounded} {interval}{(rounded > 1 ? "s" : "")} ago";
        }
    }

    public string GetTimePlayedFormatted()
    {
        if (!minsPlayed.HasValue)
            return string.Empty;

        int hours = (int)Math.Floor(minsPlayed.Value / 60f);

        if (hours == 0) return $"{minsPlayed.Value} min{(minsPlayed.Value > 1 ? "s" : "")}";
        return $"{hours} hour{(hours > 1 ? "s" : "")}";
    }

    public (string msg, Func<Task> resolution)[] GetWarnings()
    {
        List<(string, Func<Task>)> warnings = new List<(string, Func<Task>)>();

        if (folderPath.Contains(',') || folderPath.Contains('!'))
        {
            CreateFixer("Illegal Folder", "This will rename the folder to remove the illegal characters", ResolveFolderPath);
        }

        return warnings.ToArray();

        void CreateFixer(string title, string desc, Func<Task> body)
        {
            warnings.Add((
                title,
                async () => await DependencyManager.OpenYesNoModalAsync(title, desc, body, "Fixing")
            ));
        }
    }

    private async Task ResolveFolderPath()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return;

        string existing = getAbsoluteFolderLocation;

        folderPath = folderPath.Replace(",", string.Empty).Replace("!", string.Empty);
        Directory.Move(existing, getAbsoluteFolderLocation);

        await UpdateDatabaseEntry(nameof(dbo_Game.gameFolder));
    }



    // required behaviour    

    public abstract Task Launch();
    public abstract Task<string?> FetchIconFilePath();

    // overridable behaviour    

    public virtual async Task<string?> ReadLogs()
    {
        string? path = getAbsoluteLogFile;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        using (FileStream stream = new FileStream(path, new FileStreamOptions()
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.ReadWrite
        }))
        {
            const int maxBytes = 100 * 1024;
            long start = Math.Max(0, stream.Length - maxBytes);

            stream.Seek(start, SeekOrigin.Begin);

            using (StreamReader reader = new StreamReader(stream))
            {
                LinkedList<string?> log = new LinkedList<string?>();

                while (!reader.EndOfStream)
                {
                    log.AddFirst(await reader.ReadLineAsync());
                }

                return string.Join(Environment.NewLine, log);
            }
        }
    }

    // custom game specific

    public virtual Task ChangeBinaryLocation(string? to) => Task.CompletedTask;
    public virtual Task ChangeRunnerId(int? runnerId) => Task.CompletedTask;

    public virtual string PromoteTempFile(string path) => string.Empty;
    public virtual (int? selected, string[] options)? GetPossibleBinaries() => null;
}
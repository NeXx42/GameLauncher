using System.Diagnostics;
using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects;

public abstract class GameDto
{
    public enum Status
    {
        Active = 1,
        Deleted = 2,
        Placeholder = 3,
    }

    public readonly int gameId;

    public string gameName { protected set; get; }
    public string folderPath { protected set; get; }

    public string? iconPath { protected set; get; }
    public string? binaryPath { protected set; get; }

    public int? libraryId { protected set; get; }
    public int? runnerId { protected set; get; }
    public Status status { protected set; get; }

    public bool? captureLogs { protected set; get; }
    public bool? useRegionEmulation { protected set; get; }

    public DateTime? lastPlayed { protected set; get; }

    public HashSet<int> tags { protected set; get; }


    public virtual string getAbsoluteFolderLocation
    {
        get
        {
            if (libraryId == null)
                return folderPath;

            return LibraryHandler.RouteLibrary(this);
        }
    }

    public virtual string? getAbsoluteLogFile
    {
        get
        {
            if (!(captureLogs ?? false))
            {
                return null;
            }

            return $"{getAbsoluteBinaryLocation}.log";
        }
    }

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

    public GameDto(dbo_Game game, dbo_GameTag[] tags)
    {
        this.gameId = game.id;
        this.gameName = game.gameName;

        this.iconPath = game.iconPath;
        this.folderPath = game.gameFolder;
        this.binaryPath = game.executablePath;

        this.captureLogs = game.captureLogs;
        this.useRegionEmulation = game.useEmulator;
        this.lastPlayed = game.lastPlayed;

        this.runnerId = game.runnerId;
        this.libraryId = game.libraryId;
        this.status = (Status)game.status;

        this.tags = tags.Select(x => x.TagId).ToHashSet();
    }

    protected async Task UpdateDatabaseEntry(params string[] columns)
    {
        await Database_Manager.Update(new dbo_Game()
        {
            id = gameId,

            gameName = gameName,
            gameFolder = folderPath,

            captureLogs = captureLogs,
            useEmulator = useRegionEmulation ?? false,

            iconPath = iconPath,
            executablePath = binaryPath,
            lastPlayed = lastPlayed,

            runnerId = runnerId,
            libraryId = libraryId,
            status = (int)status

        }, SQLFilter.Equal(nameof(dbo_Game.id), gameId), columns);

        LibraryHandler.onGameDetailsUpdate?.Invoke(gameId);
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

        await UpdateDatabaseEntry(nameof(dbo_Game.iconPath));
        ImageManager.ClearCache(gameId);
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

        LibraryHandler.onGameDetailsUpdate?.Invoke(gameId);
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

    public string GetLastPlayedFormatted()
    {
        if (lastPlayed == null) return "Never";

        TimeSpan time = DateTime.UtcNow - lastPlayed.Value;

        if (time.TotalDays > 0) return Format(time.TotalDays, "day");
        if (time.TotalHours > 0) return Format(time.TotalHours, "hour");
        if (time.TotalMinutes > 0) return Format(time.TotalMinutes, "min");

        return Format(time.TotalSeconds, "sec");

        string Format(double time, string interval)
        {
            double rounded = Math.Ceiling(time);
            return $"{rounded} {interval}{(rounded > 1 ? "s" : "")} ago";
        }
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

    public virtual Task UpdateGameEmulationStatus(bool to) => Task.CompletedTask;
    public virtual Task UpdateCaptureLogsStatus(bool to) => Task.CompletedTask;
    public virtual Task ChangeBinaryLocation(string? to) => Task.CompletedTask;
    public virtual Task ChangeRunnerId(int? runnerId) => Task.CompletedTask;

    public virtual string PromoteTempFile(string path) => string.Empty;
    public virtual (int? selected, string[] options)? GetPossibleBinaries() => null;
}
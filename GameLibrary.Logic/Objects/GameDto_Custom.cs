using System.Diagnostics;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Objects;

public class GameDto_Custom : GameDto
{
    public GameDto_Custom(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config) : base(game, tags, config)
    {
    }

    public override async Task Launch()
    {
        await RunnerManager.RunGame(new RunnerManager.LaunchRequest()
        {
            gameId = gameId,
            path = getAbsoluteBinaryLocation,
            runnerId = runnerId,

            gameConfig = config
        });
    }

    public override async Task ChangeBinaryLocation(string? path)
    {
        string newAbsolutePath = Path.Combine(getAbsoluteFolderLocation, path!);

        if (!File.Exists(newAbsolutePath))
        {
            return;
        }

        this.binaryPath = path;
        await UpdateDatabaseEntry(nameof(dbo_Game.executablePath));
    }

    public override async Task ChangeRunnerId(int? runnerId)
    {
        this.runnerId = runnerId;
        await UpdateDatabaseEntry(nameof(dbo_Game.runnerId));
    }


    public override string PromoteTempFile(string path)
    {
        string extension = Path.GetExtension(path);
        string newName = $"{Guid.NewGuid()}{extension}";

        File.Move(path, Path.Combine(getAbsoluteFolderLocation, newName));
        return newName;
    }

    // misc

    public override (int? selected, string[] options)? GetPossibleBinaries()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return (null, []);

        List<string> binaries = Directory.GetFiles(getAbsoluteFolderLocation).Where(RunnerManager.IsUniversallyAcceptedExecutableFormat).Select(x => Path.GetFileName(x)).ToList();
        return (binaries.IndexOf(binaryPath!), binaries.ToArray());
    }

    public override Task<string?> FetchIconFilePath() => Task.FromResult(string.IsNullOrEmpty(iconPath) ? null : getAbsoluteIconPath);
}
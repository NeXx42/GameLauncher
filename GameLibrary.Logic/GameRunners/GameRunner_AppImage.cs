using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameRunners;

public class GameRunner_AppImage : IGameRunner
{
    public string[] getAcceptableExtensions => ["AppImage"];

    public Task SetupRunner(Dictionary<string, string?> configValues) => Task.CompletedTask;

    public Task<RunnerManager.GameLaunchData> InitRunDetails(RunnerManager.GameLaunchRequest game)
    {
        return Task.FromResult(new RunnerManager.GameLaunchData()
        {
            command = game.path,
            whiteListedDirs = [Path.GetDirectoryName(game.path)!]
        });
    }
}

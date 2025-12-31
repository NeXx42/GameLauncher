using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameRunners;

public interface IGameRunner
{
    public string[] getAcceptableExtensions { get; }

    public Task SetupRunner(Dictionary<string, string?> configValues); // actually install prerequisites and the like
    public Task<RunnerManager.GameLaunchData> InitRunDetails(RunnerManager.GameLaunchRequest game);
}

using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameRunners;

public class GameRunner_Wine : IGameRunner
{

    protected readonly string rootLoc;

    protected string prefixFolder;

    protected virtual string getWineExecutable => "wine";

    public virtual string[] getAcceptableExtensions => ["exe"];

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);


    public GameRunner_Wine(dbo_Runner data)
    {
        rootLoc = Path.Combine(data.runnerRoot, data.runnerName.Replace(" ", string.Empty));

        GameRunnerHelperMethods.EnsureDirectoryExists(rootLoc);
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "binaries"));
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "prefixes"));

        prefixFolder = Path.Combine(rootLoc, "prefixes", "shared");

        GameRunnerHelperMethods.EnsureDirectoryExists(prefixFolder);
    }


    public async virtual Task SetupRunner(Dictionary<string, string?> configValues) { }

    public async virtual Task<RunnerManager.GameLaunchData> InitRunDetails(RunnerManager.GameLaunchRequest game)
    {
        RunnerManager.GameLaunchData res = new RunnerManager.GameLaunchData() { command = getWineExecutable };
        res.command = getWineExecutable;
        res.arguments.AddLast(game.path);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(prefixFolder);
        res.whiteListedDirs.Add(rootLoc);

        foreach (var a in GetWineEnvironmentVariables())
            res.environmentArguments.Add(a.Key, a.Value);

        return res;
    }


    protected virtual async Task RunWine(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = getWineExecutable,

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        foreach (var a in GetWineEnvironmentVariables())
            startInfo.EnvironmentVariables[a.Key] = a.Value;

        Process process = new Process();
        process.StartInfo = startInfo;

        process.Start();
        await process.WaitForExitAsync();
    }

    protected virtual Dictionary<string, string> GetWineEnvironmentVariables()
    {
        return new Dictionary<string, string>()
        {
            { "WINEPREFIX", prefixFolder },
            {"WINEDEBUG", "-all"}
        };
    }
}

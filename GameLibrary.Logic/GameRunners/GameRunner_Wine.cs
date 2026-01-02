using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameRunners;

public class GameRunner_Wine : IGameRunner
{
    protected readonly string version;
    protected readonly string rootLoc;
    protected readonly string binaryFolder;
    protected readonly string dxvkFolder;

    protected string prefixFolder;
    protected string binaryPath;

    protected string getWineExecutable => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "bin", "wine64");
    protected string getWineLib => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "lib");

    public virtual string[] getAcceptableExtensions => ["exe"];

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);


    public GameRunner_Wine(dbo_Runner data)
    {
        version = data.runnerVersion;
        rootLoc = Path.Combine(data.runnerRoot, data.runnerName.Replace(" ", string.Empty));

        GameRunnerHelperMethods.EnsureDirectoryExists(rootLoc);
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "binaries"));
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "prefixes"));
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "DXVK"));

        dxvkFolder = Path.Combine(rootLoc, "DXVK", "latest"); // add versioning later
        binaryFolder = Path.Combine(rootLoc, "binaries", data.runnerVersion);
        prefixFolder = Path.Combine(rootLoc, "prefixes", "shared");

        GameRunnerHelperMethods.EnsureDirectoryExists(prefixFolder);
    }


    public async virtual Task SetupRunner(Dictionary<string, string?> configValues)
    {
        if (!Directory.Exists(binaryFolder))
        {
            await InstallWine();
        }

        if (!Directory.Exists(dxvkFolder))
        {
            await InstallDXVK();
        }
    }

    protected async virtual Task InstallWine() { }
    protected async virtual Task InstallDXVK() { }

    public async virtual Task<RunnerManager.GameLaunchData> InitRunDetails(RunnerManager.GameLaunchRequest game)
    {
        RunnerManager.GameLaunchData res = new RunnerManager.GameLaunchData() { command = getWineExecutable };
        res.command = getWineExecutable;
        res.arguments.AddLast(game.path);

        res.whiteListedDirs.Add(Path.GetFileName(game.path));
        res.whiteListedDirs.Add(prefixFolder);

        res.environmentArguments.Add("WINEPREFIX", prefixFolder);
        res.environmentArguments.Add("WINEDEBUG", "-all");
        res.environmentArguments.Add("DXVK_LOG_LEVEL=info", "info");
        res.environmentArguments.Add("LD_LIBRARY_PATH", getWineLib);

        return res;
    }


    protected async Task RunWine(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = getWineExecutable,

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        startInfo.EnvironmentVariables["WINEPREFIX"] = prefixFolder;
        startInfo.EnvironmentVariables["WINEDEBUG"] = "-all";
        startInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = getWineLib;

        Process process = new Process();
        process.StartInfo = startInfo;

        process.Start();
        await process.WaitForExitAsync();
    }
}

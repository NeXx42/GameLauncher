using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.GameRunners;

public class GameRunner_umu : IGameRunner
{
    private readonly string rootLoc;
    private readonly string prefixLoc;

    private readonly string version;

    private static string getRuntimeLocationRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/compatibilitytools.d");

    public string[] getAcceptableExtensions => ["exe"];

    public GameRunner_umu(dbo_Runner data)
    {
        version = data.runnerVersion;
        rootLoc = data.GetRoot();

        GameRunnerHelperMethods.EnsureDirectoryExists(rootLoc);

        prefixLoc = Path.Combine(rootLoc, "prefixes", "shared");

        GameRunnerHelperMethods.EnsureDirectoryExists(prefixLoc);
    }

    public Task SetupRunner(Dictionary<string, string?> configValues)
    {
        // getRuntimeLocationRoot / version / proton run wineboot
        return Task.CompletedTask;
    }

    public Task<RunnerManager.GameLaunchData> InitRunDetails(RunnerManager.GameLaunchRequest game)
    {
        RunnerManager.GameLaunchData res = new RunnerManager.GameLaunchData() { command = "umu-run" };

        res.arguments.AddLast(game.path);
        res.arguments.AddLast("-windowed");

        res.environmentArguments.Add("WINEPREFIX", prefixLoc);
        res.environmentArguments.Add("STEAM_COMPAT_TOOL_PATHS", getRuntimeLocationRoot);
        res.environmentArguments.Add("STEAM_COMPAT_TOOL", version);

        res.environmentArguments.Add("UMU_LOG", "debug");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        res.whiteListedDirs = [
            prefixLoc,
            getRuntimeLocationRoot,
            Path.GetDirectoryName(game.path)!,
            Path.Combine(homeDir, ".local/share/umu"),
            Path.Combine(homeDir, ".cache", "mesa_shader_cache"),
            Path.Combine(homeDir, ".cache", "AMDVLK"),
            Path.Combine(homeDir, ".cache", "dxvk"),
            Path.Combine(homeDir, ".cache", "vkd3d"),
        ];

        return Task.FromResult(res);
    }

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(Directory.GetDirectories(getRuntimeLocationRoot).Select(x => Path.GetFileName(x)).ToArray());

}

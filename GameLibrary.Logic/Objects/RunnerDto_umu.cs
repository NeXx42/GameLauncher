using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.GameRunners;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_umu : RunnerDto_Wine
{
    private readonly string rootLoc;
    private readonly string prefixLoc;

    private readonly string version;

    private static string getRuntimeLocationRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/compatibilitytools.d");

    public string[] getAcceptableExtensions => ["exe"];


    public RunnerDto_umu(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        version = runnerVersion;
        rootLoc = GetRoot();

        GameRunnerHelperMethods.EnsureDirectoryExists(rootLoc);

        prefixLoc = Path.Combine(rootLoc, "prefixes", "shared");

        GameRunnerHelperMethods.EnsureDirectoryExists(prefixLoc);
    }

    public override Task SetupRunner() => Task.CompletedTask;

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = "umu-run" };

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

    public static async Task<string[]?> GetRunnerVersions()
    {
        if (!Directory.Exists(getRuntimeLocationRoot))
        {
            await DependencyManager.OpenYesNoModal("UMU failure", "No runtimes found");
            return ["INVALID"];
        }

        string[] files = Directory.GetDirectories(getRuntimeLocationRoot);
        return files.Select(x => Path.GetFileName(x)).ToArray();
    }

}

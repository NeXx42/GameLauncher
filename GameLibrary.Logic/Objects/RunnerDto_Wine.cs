using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_Wine : RunnerDto
{
    protected readonly string rootLoc;

    protected string prefixFolder;
    protected virtual string getWineExecutable => "wine";

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);


    protected override string[] GetAcceptableExtensions() => ["exe"];

    public RunnerDto_Wine(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        rootLoc = GetRoot();

        GameRunnerHelperMethods.EnsureDirectoryExists(rootLoc);
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "binaries"));
        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "prefixes"));

        prefixFolder = Path.Combine(rootLoc, "prefixes", "shared");

        GameRunnerHelperMethods.EnsureDirectoryExists(prefixFolder);
    }

    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = getWineExecutable };
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



    public override async Task SharePrefixDocuments(string path)
    {
        if (!Directory.Exists(prefixFolder))
            throw new Exception("Profile hasnt been ran yet");

        string[] users = Directory.GetDirectories(Path.Combine(prefixFolder, "drive_c", "users"));

        foreach (string usr in users)
            HandleUser(usr);

        await AddOrUpdateConfigValue(RunnerConfigValues.Wine_SharedDocuments, path);

        void HandleUser(string usrPath)
        {
            string usrName = Path.GetFileName(usrPath)!;

            // ensure share directory is correct
            string shareRoot = Path.Combine(path, usrName).CreateDirectoryIfNotExists();
            string shareDocuments = Path.Combine(shareRoot, "Documents").CreateDirectoryIfNotExists();

            Path.Combine(shareRoot, "AppData").CreateDirectoryIfNotExists();
            string shareLocal = Path.Combine(shareRoot, "AppData", "Local").CreateDirectoryIfNotExists();
            string shareRoaming = Path.Combine(shareRoot, "AppData", "Roaming").CreateDirectoryIfNotExists();

            HandleSymlink(Path.Combine(usrPath, "Documents"), shareDocuments);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Local"), shareLocal);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Roaming"), shareRoaming);

            void HandleSymlink(string prefixLoc, string sharedLoc)
            {
                prefixLoc.CreateDirectoryIfNotExists(); // in case future games need this and it doesn't currently exist

                Directory.Delete(prefixLoc, true);
                Directory.CreateSymbolicLink(prefixLoc, sharedLoc);
            }
        }
    }
}

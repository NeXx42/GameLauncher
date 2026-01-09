using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_Wine : RunnerDto
{
    protected readonly string rootLoc;
    protected readonly string prefixRoot;

    protected virtual string getSharedPrefixFolder => Path.Combine(prefixRoot, "shared");
    protected virtual string getWineExecutable => "wine";

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);


    protected override string[] GetAcceptableExtensions() => ["exe"];

    public RunnerDto_Wine(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        rootLoc = GetRoot().CreateDirectoryIfNotExists();
        Path.Combine(rootLoc, "binaries").CreateDirectoryIfNotExists();
        Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();

        prefixRoot = Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();
        getSharedPrefixFolder.CreateDirectoryIfNotExists();
    }

    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = getWineExecutable };
        res.command = getWineExecutable;

        string prefixName = "shared";

        if (game.gameConfig != null)
        {
            if (game.gameConfig.GetBoolean(Game_Config.Wine_ExplorerLaunch, false))
            {
                res.arguments.AddLast("explorer");
                res.arguments.AddLast("/desktop=Game,800x600");
            }

            if (game.gameConfig.GetBoolean(Game_Config.Wine_IsolatedPrefix, false))
            {
                prefixName = game.path;
                prefixName = prefixName.Replace("!", string.Empty);
                prefixName = prefixName.Replace(",", string.Empty);
                prefixName = prefixName.Replace(" ", string.Empty);
                prefixName = prefixName.Replace("_", string.Empty);
                prefixName = prefixName.Replace("'", string.Empty);
                prefixName = prefixName.Replace("/", "_");

                // from this i can later split on _ and get just the "folder name" or close enough
            }
        }

        res.arguments.AddLast(game.path);

        if (game.gameConfig?.GetBoolean(Enums.Game_Config.Wine_Windowed, false) ?? false)
        {
            res.arguments.AddLast("-windowed");
            res.arguments.AddLast("-window");
            res.arguments.AddLast("-w");
        }

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(Path.Combine(prefixRoot, prefixName));
        res.whiteListedDirs.Add(rootLoc);

        res.environmentArguments.Add("WINEPREFIX", Path.Combine(prefixRoot, prefixName));

        AddLogging(res, game.gameConfig?.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off);
        return res;
    }

    protected virtual void AddLogging(RunnerManager.LaunchArguments args, LoggingLevel level)
    {
        string? logString = null;

        switch (level)
        {
            case LoggingLevel.Low:
                logString = "+err";
                break;

            case LoggingLevel.High:
                logString = "+err,+warn";
                break;

            case LoggingLevel.Everything:
                logString = "+all";
                break;
        }

        if (string.IsNullOrEmpty(logString))
            return;

        args.environmentArguments.Add("WINEDEBUG", logString);
    }


    protected virtual async Task RunWine(params string[] args)
    {
        await RunnerManager.ExecuteRunRequest(await InitRunDetails(new RunnerManager.LaunchRequest()
        {
            identifier = "wine",
            path = "reg",
        }), null).WaitForExitAsync();
    }

    public override async Task SharePrefixDocuments(string path)
    {
        string prefixFolder = Path.Combine(prefixRoot, "shared");

        if (!Directory.Exists(prefixFolder))
            throw new Exception("Profile hasnt been ran yet");

        string[] users = Directory.GetDirectories(Path.Combine(prefixFolder, "drive_c", "users"));

        foreach (string usr in users)
            HandleUser(usr);

        await globalRunnerValues.SaveValue(RunnerConfigValues.Wine_SharedDocuments, path);

        void HandleUser(string usrPath)
        {
            string usrName = Path.GetFileName(usrPath)!;

            // ensure share directory is correct
            string shareRoot = Path.Combine(path, usrName).CreateDirectoryIfNotExists();
            string shareDocuments = Path.Combine(shareRoot, "Documents").CreateDirectoryIfNotExists();

            Path.Combine(shareRoot, "AppData").CreateDirectoryIfNotExists();
            string shareLocal = Path.Combine(shareRoot, "AppData", "Local").CreateDirectoryIfNotExists();
            string shareRoaming = Path.Combine(shareRoot, "AppData", "Roaming").CreateDirectoryIfNotExists();
            string shareLocalLow = Path.Combine(shareRoot, "AppData", "LocalLow").CreateDirectoryIfNotExists();

            HandleSymlink(Path.Combine(usrPath, "Documents"), shareDocuments);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Local"), shareLocal);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Roaming"), shareRoaming);
            HandleSymlink(Path.Combine(usrPath, "AppData", "LocalLow"), shareLocalLow);

            void HandleSymlink(string prefixLoc, string sharedLoc)
            {
                prefixLoc.CreateDirectoryIfNotExists(); // in case future games need this and it doesn't currently exist

                Directory.Delete(prefixLoc, true);
                Directory.CreateSymbolicLink(prefixLoc, sharedLoc);
            }
        }
    }
}

using CSharpSqliteORM;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto
{
    public enum RunnerType
    {
        AppImage = 0,
        Wine = 1,
        Wine_GE = 2,
        umu_Launcher = 3,
    }

    public enum RunnerConfigValues
    {
        Wine_Prefix,
        Wine_SharedDocuments,

        Generic_Sandbox_BlockNetwork,
        Generic_Sandbox_IsolateFilesystem,
    }


    public int runnerId { private set; get; }
    public RunnerType runnerType { set; get; }

    public string runnerName { set; get; }

    public string runnerRoot { set; get; }
    public string runnerVersion { set; get; }

    public ConfigProvider<RunnerConfigValues> globalRunnerValues { private set; get; }

    public RunnerDto(dbo_Runner runner, dbo_RunnerConfig[] configValues)
    {
        this.runnerId = runner.runnerId;
        this.runnerType = (RunnerType)runner.runnerType;

        this.runnerName = runner.runnerName;

        this.runnerRoot = runner.runnerRoot;
        this.runnerVersion = runner.runnerVersion;

        globalRunnerValues = new ConfigProvider<RunnerConfigValues>(configValues.Select(x => (x.settingKey, x.settingValue)), SaveConfigValue, DeleteConfigValue);
    }

    public static RunnerDto Create(dbo_Runner runner, dbo_RunnerConfig[] configValues)
    {
        switch ((RunnerType)runner.runnerType)
        {
            case RunnerType.Wine: return new RunnerDto_Wine(runner, configValues);
            case RunnerType.Wine_GE: return new RunnerDto_WineGE(runner, configValues);
            case RunnerType.umu_Launcher: return new RunnerDto_umu(runner, configValues);

            default: return new RunnerDto(runner, configValues);
        }
    }

    // database

    public async Task UpdateDatabaseEntry(params string[] columns)
    {
        dbo_Runner dbo = new dbo_Runner()
        {
            runnerId = runnerId,
            runnerType = (int)runnerType,

            runnerName = runnerName,

            runnerRoot = runnerRoot,
            runnerVersion = runnerVersion,
        };

        await Database_Manager.Update(dbo, SQLFilter.Equal(nameof(dbo_Runner.runnerId), runnerId), columns);
    }

    private async Task SaveConfigValue(string key, string val)
    {
        dbo_RunnerConfig dbo = new dbo_RunnerConfig()
        {
            runnerId = runnerId,
            settingKey = key.ToString(),
            settingValue = val
        };

        await Database_Manager.AddOrUpdate(dbo, SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).Equal(nameof(dbo_RunnerConfig.settingKey), key), nameof(dbo_RunnerConfig.settingValue));
    }

    private async Task DeleteConfigValue(string key)
        => await Database_Manager.Delete<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).Equal(nameof(dbo_RunnerConfig.settingKey), key));


    // matchers

    public static async Task<string[]?> GetVersionsForRunnerTypes(int typeId)
    {
        switch ((RunnerType)typeId)
        {
            case RunnerType.Wine: return await RunnerDto_Wine.GetRunnerVersions();
            case RunnerType.Wine_GE: return await RunnerDto_WineGE.GetRunnerVersions();
            case RunnerType.umu_Launcher: return await RunnerDto_umu.GetRunnerVersions();
        }

        return null;
    }

    // logic

    public string GetRoot() => Path.Combine(runnerRoot, runnerName.Replace(" ", string.Empty));
    public virtual async Task SharePrefixDocuments(string path) => throw new Exception("Invalid profile");

    public bool IsValidExtension(string path)
    {
        string[] allowed = GetAcceptableExtensions();

        foreach (string extension in allowed)
            if (!path.EndsWith($".{extension}", StringComparison.CurrentCultureIgnoreCase))
                return false;

        return true;
    }

    // Launching

    protected virtual string[] GetAcceptableExtensions() => ["AppImage"];

    public virtual Task SetupRunner() => Task.CompletedTask;

    public virtual Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest req)
    {
        return Task.FromResult(new RunnerManager.LaunchArguments()
        {
            command = req.path,
            whiteListedDirs = [Path.GetDirectoryName(req.path)!]
        });
    }
}

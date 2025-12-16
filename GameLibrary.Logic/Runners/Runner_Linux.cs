using System.Diagnostics;
using System.Runtime.InteropServices;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Runners;

public class Runner_Linux : IRunner
{
    public async Task<ProcessStartInfo> Run(dbo_Game game)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        LaunchOptions wineOptions = await GetWineOptions(game);

        await EmulateRegion(info, game);
        await Sandbox(info, wineOptions);
        await EmbedWine(info, wineOptions);

        return info;
    }

    private Task EmulateRegion(ProcessStartInfo info, dbo_Game game)
    {
        if (game.useEmulator)
        {
            info.EnvironmentVariables["LANG"] = "ja_JP.UTF-8";
            info.EnvironmentVariables["LC_ALL"] = "ja_JP.UTF-8";
            info.EnvironmentVariables["LC_CTYPE"] = "ja_JP.UTF-8";
        }

        return Task.CompletedTask;
    }

    private async Task<LaunchOptions> GetWineOptions(dbo_Game game)
    {
        LaunchOptions options = new LaunchOptions();

        if (game.wineProfile.HasValue)
        {
            dbo_WineProfile? profile = await DatabaseHandler.GetItem<dbo_WineProfile>(QueryBuilder.SQLEquals(nameof(dbo_WineProfile.id), game.wineProfile.Value));

            if (profile == null)
                throw new Exception($"Profile doesn't exist");

            options.prefix = profile!.profileDirectory!;
        }

        options.gameFolder = await game.GetAbsoluteFolderLocation();
        options.gameExecutable = Path.Combine(options.gameFolder, game.executablePath!);

        return options;
    }

    private async Task EmbedWine(ProcessStartInfo info, LaunchOptions options)
    {
        if (!string.IsNullOrEmpty(options.prefix))
        {
            info.EnvironmentVariables["WINEPREFIX"] = options.prefix;
        }

        if (options.didSandbox)
        {
            info.ArgumentList.Add("wine");
        }
        else
        {
            info.FileName = "wine";
        }

        info.ArgumentList.Add(options.gameExecutable!);
    }

    private async Task Sandbox(ProcessStartInfo info, LaunchOptions options)
    {
        if (!await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Enabled, false))
        {
            options.didSandbox = false;
            return;
        }

        bool isolateFileSystem = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, false);

        if (isolateFileSystem)
        {
            if (!string.IsNullOrEmpty(options.prefix))
            {
                info.ArgumentList.Add($"--whitelist={options.prefix}");
            }

            info.ArgumentList.Add($"--whitelist={options.gameFolder}");
            info.ArgumentList.Add("--private-dev");
        }

        if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Networking, true))
        {
            info.ArgumentList.Add("--net=none");
        }

        info.FileName = "firejail";
        options.didSandbox = true;
    }

    public Task<Runner_Game> LaunchGame(Process process, string logPath)
    {
        return Task.FromResult((Runner_Game)new Runner_LinuxGame(logPath, process));
    }

    private class LaunchOptions
    {
        public string? prefix;
        public string? gameFolder;
        public string? gameExecutable;

        public bool didSandbox;
    }


    public class Runner_LinuxGame : Runner_Game
    {
        public Runner_LinuxGame(string logPath, Process p) : base(logPath, p)
        {
        }

        public override void PostRun()
        {
            groupId = process.Id;
            //setpgid(groupId.Value, groupId.Value);
        }
    }
}

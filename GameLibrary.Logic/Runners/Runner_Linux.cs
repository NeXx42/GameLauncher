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

        string[] requiredLocations = await GetRequiredWineDirectories(game);

        await EmulateRegion(info, game);
        bool didSandbox = await Sandbox(info, requiredLocations);
        await EmbedWine(info, game, didSandbox);

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

    private async Task<string[]> GetRequiredWineDirectories(dbo_Game game)
    {
        List<string> dirs = new List<string>();

        if (game.wineProfile.HasValue)
        {
            dbo_WineProfile? profile = await DatabaseHandler.GetItem<dbo_WineProfile>(QueryBuilder.SQLEquals(nameof(dbo_WineProfile.id), game.wineProfile.Value));

            if (profile == null)
                throw new Exception($"Profile doesn't exist");

            dirs.Add(profile!.profileDirectory!);
        }

        dirs.Add(await game.GetAbsoluteFolderLocation());
        return dirs.ToArray();
    }

    private async Task EmbedWine(ProcessStartInfo info, dbo_Game game, bool didSandbox)
    {
        if (didSandbox)
        {
            info.ArgumentList.Add("wine");
        }
        else
        {
            info.FileName = "wine";
        }

        info.ArgumentList.Add(await game.GetAbsoluteExecutableLocation());
    }

    private async Task<bool> Sandbox(ProcessStartInfo info, string[] whitelistedLocation)
    {
        if (!await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Enabled, false))
            return false;

        bool isolateFileSystem = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, false);

        if (isolateFileSystem)
        {
            foreach (string str in whitelistedLocation)
            {
                info.ArgumentList.Add($"--whitelist={str}");
            }

            info.ArgumentList.Add("--private-dev");
        }

        if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Networking, true))
        {
            info.ArgumentList.Add("--net=none");
        }

        info.FileName = "firejail";
        return true;
    }

    public Task<Runner_Game> LaunchGame(Process process, string logPath)
    {
        return Task.FromResult((Runner_Game)new Runner_LinuxGame(logPath, process));
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

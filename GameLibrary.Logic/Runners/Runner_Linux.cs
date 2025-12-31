using System.Diagnostics;
using CSharpSqliteORM;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.Runners;

public class Runner_Linux : IRunner
{
    public enum RunnerType
    {
        Wine = 0,
        Proton = 1
    }

    public async Task<ProcessStartInfo> Run(IGameDto game)
    {
        return null;
        //ProcessStartInfo info = new ProcessStartInfo();
        //
        //await EmulateRegion(info, game);
        //await Sandbox(info, wineOptions);
        //
        //switch (wineOptions.runnerType)
        //{
        //    case RunnerType.Wine:
        //        await EmbedWine(info, wineOptions);
        //        break;
        //
        //    case RunnerType.Proton:
        //        await EmbedProton(info, wineOptions);
        //        break;
        //
        //    default:
        //        throw new Exception("Invalid runner type");
        //}
        //
        //
        //return info;
    }

    private Task EmulateRegion(ProcessStartInfo info, IGameDto game)
    {
        if (game.useRegionEmulation)
        {

        }

        return Task.CompletedTask;
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
            foreach (string str in options.whitelistDirectories)
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
        options.didSandbox = true;
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

    private async Task EmbedProton(ProcessStartInfo info, LaunchOptions options)
    {
        info.EnvironmentVariables["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = options.steamLocation;
        info.EnvironmentVariables["STEAM_COMPAT_DATA_PATH"] = options.prefix;
        //WINEDEBUG

        if (options.didSandbox)
        {
            info.ArgumentList.Add(options.runnerExecutable);
            info.ArgumentList.Add("run");
            info.ArgumentList.Add(options.gameExecutable);
        }
        else
        {
            info.FileName = options.runnerExecutable;
            info.ArgumentList.Add("run");
            info.ArgumentList.Add(options.gameExecutable);
        }
    }



    public Task<Runner_Game> LaunchGame(IGameDto game, Process process, string logPath)
    {
        return Task.FromResult((Runner_Game)new Runner_LinuxGame(game, logPath, process));
    }

    private class LaunchOptions
    {
        public string? prefix;
        public string? gameExecutable;
        public string? runnerExecutable;
        public string? steamLocation;

        public bool didSandbox;

        public RunnerType? runnerType;

        public List<string> whitelistDirectories = new List<string>();
    }


    public class Runner_LinuxGame : Runner_Game
    {
        public Runner_LinuxGame(IGameDto game, string logPath, Process p) : base(game, logPath, p)
        {
        }

        public override void PostRun()
        {
            groupId = process.Id;
            //setpgid(groupId.Value, groupId.Value);
        }
    }
}

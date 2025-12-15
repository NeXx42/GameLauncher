using System.Diagnostics;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Runners;

public class Runner_Linux : IRunner
{
    public const string WINE_GLOBAL_ISOLATION_FOLDER_NAME = "_GlobalShare";

    public async Task<ProcessStartInfo> Run(dbo_Game game)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        (bool didSandbox, bool useRelativePath) = await Sandbox(info, game);

        await EmulateRegion(info, game);
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

    private async Task EmbedWine(ProcessStartInfo info, dbo_Game game, bool didSandbox)
    {
        if (game.wineProfile.HasValue)
        {
            dbo_WineProfile? profile = await DatabaseHandler.GetItem<dbo_WineProfile>(QueryBuilder.SQLEquals(nameof(dbo_WineProfile.id), game.wineProfile.Value));

            if (profile == null)
                throw new Exception($"Profile doesn't exist");

            info.EnvironmentVariables["WINEPREFIX"] = profile!.profileDirectory!;
        }

        if (!didSandbox)
        {

            info.FileName = "wine";
            info.Arguments = await game.GetAbsoluteExecutableLocation();
        }
        else
        {
            string gamePath = await game.GetAbsoluteExecutableLocation();

            info.ArgumentList.Add("wine");
            info.ArgumentList.Add(gamePath);
        }
    }

    private async Task<(bool, bool)> Sandbox(ProcessStartInfo info, dbo_Game game)
    {
        if (!await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Enabled, false))
            return (false, false);

        bool isolateFileSystem = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, false);

        if (false)
        {
            info.ArgumentList.Add($"--private=\"{await game.GetLibraryLocation()}\"");
        }

        if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Networking, true))
        {
            info.ArgumentList.Add("--net=none");
        }

        info.FileName = "firejail";
        return (true, isolateFileSystem);
    }
}

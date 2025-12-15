using System.Diagnostics;
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
        await EmbedWine(info, game, didSandbox, useRelativePath);

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

    private async Task EmbedWine(ProcessStartInfo info, dbo_Game game, bool didSandbox, bool useRelativeDir)
    {
        string winePrefix = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Wine_IsolationDirectory, string.Empty);

        if (!string.IsNullOrEmpty(winePrefix))
        {
            info.EnvironmentVariables["WINEPREFIX"] = Path.Combine(winePrefix, ".wine");
        }

        if (!didSandbox)
        {

            info.FileName = "wine";
            info.Arguments = await game.GetExecutableLocation();
        }
        else
        {
            string gamePath = await game.GetExecutableLocation();

            if (useRelativeDir)
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                info.EnvironmentVariables["WINEPREFIX"] = Path.Combine(userPath, ".wine");

                gamePath = Path.Combine(userPath, game.gameFolder, game.executablePath);
            }

            info.Arguments += $" wine \"{gamePath}\"";
        }
    }

    private async Task<(bool, bool)> Sandbox(ProcessStartInfo info, dbo_Game game)
    {
        if (!await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Enabled, false))
            return (false, false);

        string firejailArguments = "";
        bool isolateFileSystem = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, false);

        if (isolateFileSystem)
        {
            firejailArguments += $" --private=\"{await game.GetLibraryLocation()}\"";
        }

        if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Networking, true))
        {
            firejailArguments += " --net=none";
        }

        info.FileName = "firejail";
        info.Arguments = $"{firejailArguments}";
        return (true, isolateFileSystem);
    }
}

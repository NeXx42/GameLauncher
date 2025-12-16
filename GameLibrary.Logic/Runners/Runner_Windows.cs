using System.Diagnostics;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Runners;

public class Runner_Windows : IRunner
{
    public async Task<ProcessStartInfo> Run(dbo_Game game)
    {
        ProcessStartInfo info = await GetFileToRun(game);
        await SandboxGame(info);

        return info;
    }

    private static async Task<ProcessStartInfo> GetFileToRun(dbo_Game game)
    {
        string realPath = await game.GetAbsoluteExecutableLocation();

        if (!File.Exists(realPath))
        {
            throw new Exception("Path doesnt exist - " + realPath);
        }

        if (game.useEmulator)
        {
            string emulatorPath = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.EmulatorPath, string.Empty);
            return new ProcessStartInfo()
            {
                FileName = emulatorPath,
                Arguments = $"-run \"{realPath}\""
            };
        }

        return new ProcessStartInfo()
        {
            FileName = realPath,
            Arguments = ""
        };
    }

    private static async Task SandboxGame(ProcessStartInfo info)
    {
        string sandboxieLoc = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Windows_SandieboxLocation, string.Empty);
        string sandboxieBox = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Windows_SandieboxBox, string.Empty);

        if (string.IsNullOrEmpty(sandboxieBox) || string.IsNullOrEmpty(sandboxieLoc))
            return;

        info.Arguments = $"/box:{sandboxieBox} \"{info.FileName}\" {info.Arguments}";
        info.FileName = sandboxieLoc;
    }

    public Task<Runner_Game> LaunchGame(Process startInfo, string logPath)
    {
        return Task.FromResult((Runner_Game)new Runner_WindowsGame(logPath, startInfo));
    }



    public class Runner_WindowsGame : Runner_Game
    {
        public Runner_WindowsGame(string logPath, Process p) : base(logPath, p)
        {
        }
    }
}

using System.Diagnostics;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.Runners;

public class Runner_Windows : IRunner
{
    public async Task<ProcessStartInfo> Run(IGameDto game)
    {
        ProcessStartInfo info = await GetFileToRun(game);
        await SandboxGame(info);

        return info;
    }

    private static async Task<ProcessStartInfo> GetFileToRun(IGameDto game)
    {
        if (!File.Exists(game.getAbsoluteBinaryLocation))
        {
            throw new Exception("Path doesnt exist - " + game.getAbsoluteBinaryLocation);
        }

        if (game.useRegionEmulation)
        {
            string emulatorPath = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.EmulatorPath, string.Empty);
            return new ProcessStartInfo()
            {
                FileName = emulatorPath,
                Arguments = $"-run \"{game.getAbsoluteBinaryLocation}\""
            };
        }

        return new ProcessStartInfo()
        {
            FileName = game.getAbsoluteBinaryLocation,
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

    public Task<Runner_Game> LaunchGame(IGameDto game, Process startInfo, string logPath)
    {
        return Task.FromResult((Runner_Game)new Runner_WindowsGame(game, logPath, startInfo));
    }



    public class Runner_WindowsGame : Runner_Game
    {
        public Runner_WindowsGame(IGameDto game, string logPath, Process p) : base(game, logPath, p)
        {
        }
    }
}

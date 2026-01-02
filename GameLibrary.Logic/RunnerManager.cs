using System.Diagnostics;
using System.Runtime.InteropServices;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.GameEmbeds;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic;

public static class RunnerManager
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

        Generic_Sandbox_BlockNetwork,
        Generic_Sandbox_IsolateFilesystem,
    }

    private static SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
    private static Dictionary<string, ActiveProcess> activeGames = new Dictionary<string, ActiveProcess>();

    public static Action<string, bool>? onGameStatusChange;

    public static bool IsBinaryRunning(string dir) => activeGames.ContainsKey(dir);

    public static bool IsUniversallyAcceptedExecutableFormat(string path)
    {
        return path.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) ||
            path.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase) ||
            path.EndsWith(".AppImage", StringComparison.CurrentCultureIgnoreCase);
    }


    public static async Task RunGame(GameLaunchRequest game)
    {
        if (activeGames.TryGetValue(game.path, out ActiveProcess? process))
        {
            if (process?.IsActive ?? false)
            {
                throw new Exception("Game is already running");
            }

            activeGames.Remove(game.path);
        }

        await _mutex.WaitAsync();

        try
        {
            dbo_Runner? selectedRunner = await Database_Manager.GetItem<dbo_Runner>(game.runnerId.HasValue ?
                SQLFilter.Equal(nameof(dbo_Runner.runnerId), game.runnerId.Value) :
                SQLFilter.OrderAsc(nameof(dbo_Runner.runnerId)));

            if (selectedRunner == null)
            {
                throw new Exception($"Couldn't find runner in the database. used id {game.runnerId ?? -1}");
            }

            IGameRunner? runner = await GetAppropriateRunner(selectedRunner);

            if (runner == null)
            {
                throw new Exception($"Invalid runner type - {selectedRunner.runnerId}");
            }

            if (game.gameId.HasValue && !File.Exists(game.path))
            {
                throw new Exception($"File doesn't exist - {game.path}");
            }

            if (!GameRunnerHelperMethods.IsValidExtension(game.path, runner))
            {
                throw new Exception($"Invalid file for the runner - {game.path}");
            }

            LibraryHandler.TryGetCachedGame(game.gameId, out GameDto? gameDto);
            Dictionary<string, string?> args = await GetRunnerArguments(game.gameId, selectedRunner.runnerId);

            await runner.SetupRunner(args);
            GameLaunchData req = await runner.InitRunDetails(game);

            await HandleEmbeds(game, req, args);
            ExecuteRunRequest(req, game.path, gameDto?.getAbsoluteLogFile);

            if (gameDto != null)
            {
                if (string.IsNullOrEmpty(gameDto!.iconPath))
                {
                    await OverlayManager.LaunchOverlay(gameDto!.gameId);
                }

                await gameDto.UpdateLastPlayed();
            }
        }
        catch (Exception e)
        {
            await DependencyManager.uiLinker!.OpenYesNoModal("Failed to launch game!", e.Message);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public static async Task RunWineTricks(int runnerId)
    {
        dbo_Runner runnerDto = (await Database_Manager.GetItem<dbo_Runner>(SQLFilter.Equal(nameof(dbo_Runner.runnerId), runnerId)))!;
        IGameRunner runner = (await GetAppropriateRunner(runnerDto))!;

        Dictionary<string, string?> args = await GetRunnerArguments(null, runnerId);

        await runner.SetupRunner(args);
        GameLaunchData req = await runner.InitRunDetails(new GameLaunchRequest() { runnerId = runnerId });

        req.command = "wine";
        req.arguments.AddFirst("winecfg");

        ExecuteRunRequest(req, "winecfg", null);
    }


    private static async Task<Dictionary<string, string?>> GetRunnerArguments(int? gameId, int runnerId)
    {
        Dictionary<string, string?> args = new Dictionary<string, string?>();

        dbo_RunnerConfig[] configValues = await Database_Manager.GetItems<dbo_RunnerConfig>(
            SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).
            IsNull(nameof(dbo_RunnerConfig.gameId)));

        args = configValues.ToDictionary(x => x.settingKey, x => x.settingValue);

        if (gameId.HasValue)
        {
            dbo_RunnerConfig[] gameConfigValues = await Database_Manager.GetItems<dbo_RunnerConfig>(
                SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).
                Equal(nameof(dbo_RunnerConfig.gameId), gameId));

            foreach (dbo_RunnerConfig configVal in gameConfigValues)
            {
                if (args.ContainsKey(configVal.settingKey))
                {
                    args[configVal.settingKey] = configVal.settingValue;
                }
                else
                {
                    args.Add(configVal.settingKey, configVal.settingValue);
                }
            }
        }

        return args;
    }

    private static async Task HandleEmbeds(GameLaunchRequest req, GameLaunchData dat, Dictionary<string, string?> args)
    {
        List<IGameEmbed> embeds = new List<IGameEmbed>();

        if (req.gameId.HasValue)
        {
            GameDto? game = LibraryHandler.TryGetCachedGame(req.gameId.Value);

            if (game != null)
            {
                if (game.useRegionEmulation ?? false)
                    embeds.Add(new GameEmbed_Locale());
            }
        }


        if (ConfigHandler.isOnLinux)
        {
            if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Enabled, false))
            {
                embeds.Add(new GameEmbed_Firejail());

                if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_Networking, false))
                {
                    args.AddOrOverride(RunnerConfigValues.Generic_Sandbox_BlockNetwork, true);
                }

                if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, false))
                {
                    args.AddOrOverride(RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true);
                }
            }
        }
        else
        {

        }


        embeds = embeds.OrderBy(x => x.getPriority).ToList();

        foreach (IGameEmbed embed in embeds)
            embed.Embed(dat, args);
    }

    private static void ExecuteRunRequest(GameLaunchData req, string identifier, string? logFile)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = req.command;

        info.UseShellExecute = false;
        info.RedirectStandardError = true;
        info.RedirectStandardOutput = true;

        foreach (var arg in req.arguments)
        {
            if (string.IsNullOrEmpty(arg))
                continue;

            info.ArgumentList.Add(arg);
        }

        foreach (var arg in req.environmentArguments)
        {
            info.EnvironmentVariables.Add(arg.Key, arg.Value);
        }

        Process process = new Process();
        process.StartInfo = info;
        process.EnableRaisingEvents = true;

        // ActiveProcess - starts the process
        activeGames.Add(identifier, new ActiveProcess(identifier, process, logFile));
        onGameStatusChange?.Invoke(identifier, true);
    }


    private static async Task<IGameRunner?> GetAppropriateRunner(dbo_Runner runner)
    {
        switch ((RunnerType)runner.runnerType)
        {
            case RunnerType.AppImage: return new GameRunner_AppImage();
            case RunnerType.Wine: return new GameRunner_Wine(runner);
            case RunnerType.Wine_GE: return new GameRunner_WineGE(runner);
            case RunnerType.umu_Launcher: return new GameRunner_umu(runner);
        }

        return null;
    }

    public static void KillProcess(string identifier)
    {
        if (activeGames.ContainsKey(identifier))
            activeGames[identifier].Dispose();
    }

    public static void OnExitProcess(string identifier)
    {
        activeGames.Remove(identifier);
        onGameStatusChange?.Invoke(identifier, false);
    }



    // db stuff


    public static async Task<List<(int id, string name)>> GetRunnerProfiles()
        => (await Database_Manager.GetItems<dbo_Runner>(SQLFilter.OrderAsc(nameof(dbo_Runner.runnerId)))).Select(x => (x.runnerId, x.runnerName)).ToList();

    public static async Task<dbo_Runner?> GetRunnerProfile(int runnerId)
        => await Database_Manager.GetItem<dbo_Runner>(SQLFilter.Equal(nameof(dbo_Runner.runnerId), runnerId));

    public static async Task<dbo_RunnerConfig[]> GetRunnerConfigValues(int runnerId)
        => await Database_Manager.GetItems<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId));


    public static async Task CreateProfile(int? runnerId, string title, string path, int typeId, string version)
    {
        dbo_Runner profile = new dbo_Runner()
        {
            runnerId = runnerId ?? -1,
            runnerName = title,
            runnerRoot = path,
            runnerType = typeId,
            runnerVersion = version
        };

        if (runnerId.HasValue)
        {
            await Database_Manager.AddOrUpdate(profile, SQLFilter.Equal(nameof(dbo_Runner.runnerId), runnerId));
        }
        else
        {
            await Database_Manager.InsertItem(profile);
        }
    }

    public static async Task<string[]?> GetVersionsForRunnerTypes(int typeId)
    {
        switch ((RunnerType)typeId)
        {
            case RunnerType.Wine: return await GameRunner_Wine.GetRunnerVersions();
            case RunnerType.Wine_GE: return await GameRunner_WineGE.GetRunnerVersions();
            case RunnerType.umu_Launcher: return await GameRunner_umu.GetRunnerVersions();
        }

        return null;
    }




    public struct GameLaunchRequest
    {
        public int? gameId;
        public int? runnerId;
        public string path;
    }


    public class GameLaunchData
    {
        public List<string> whiteListedDirs = new List<string>();

        public required string command;
        public LinkedList<string> arguments = new LinkedList<string>();
        public Dictionary<string, string> environmentArguments = new Dictionary<string, string>();
    }

    private class ActiveProcess : IDisposable
    {
        private readonly string identifier;
        private readonly Process process;

        private StreamWriter? writer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public bool IsActive => !process.HasExited;

        public ActiveProcess(string identifier, Process process, string? logFile)
        {
            if (!string.IsNullOrEmpty(logFile))
            {
                writer = new StreamWriter(logFile, System.Text.Encoding.ASCII, new FileStreamOptions()
                {
                    Mode = FileMode.OpenOrCreate,
                    Access = FileAccess.ReadWrite,
                    Share = FileShare.ReadWrite
                });
            }

            this.identifier = identifier;
            this.process = process;

            process.Exited += HandleExit;

            process.OutputDataReceived += OnOutput;
            process.ErrorDataReceived += OnOutput;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private async void OnOutput(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                if (writer != null)
                {
                    await _writeLock.WaitAsync();

                    try
                    {
                        await writer.WriteLineAsync(args.Data);
                        await writer.FlushAsync();
                    }
                    finally
                    {
                        _writeLock.Release();
                    }
                }

                Console.WriteLine(args.Data);
            }
        }

        private void HandleExit(object? sender, EventArgs args)
        {
            RunnerManager.OnExitProcess(identifier);
        }


        public void Dispose()
        {
            KillProcessTree(process.Id);
            writer?.Close();
        }


        [DllImport("libc")]
        private static extern int kill(int pid, int sig);

        public static void KillProcessTree(int pid)
        {
            var children = new List<int>();
            try
            {
                var psi = new ProcessStartInfo("pgrep", $"-P {pid}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using var pgrep = Process.Start(psi);
                string? line;

                while ((line = pgrep!.StandardOutput.ReadLine()) != null)
                {
                    if (int.TryParse(line, out int childPid))
                        children.Add(childPid);
                }

                pgrep.WaitForExit();
            }
            catch { }

            foreach (var child in children)
                KillProcessTree(child);

            try
            {
                kill(pid, 9);
            }
            catch { }
        }
    }
}

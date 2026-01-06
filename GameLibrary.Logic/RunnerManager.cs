using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameEmbeds;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic;

public static class RunnerManager
{
    private static SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

    private static Dictionary<int, RunnerDto> cachedRunners = new Dictionary<int, RunnerDto>();
    private static Dictionary<string, ActiveProcess> activeGames = new Dictionary<string, ActiveProcess>();

    public static Action<string, bool>? onGameStatusChange;

    public static bool IsBinaryRunning(string dir) => activeGames.ContainsKey(dir);

    public static bool IsUniversallyAcceptedExecutableFormat(string path)
    {
        return path.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) ||
            path.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase) ||
            path.EndsWith(".AppImage", StringComparison.CurrentCultureIgnoreCase);
    }

    public static async Task Init()
    {
        await RecacheRunners();
    }


    public static async Task RunGame(LaunchRequest launchRequest)
    {
        if (activeGames.TryGetValue(launchRequest.path, out ActiveProcess? process))
        {
            if (process?.IsActive ?? false)
            {
                throw new Exception("Game is already running");
            }

            activeGames.Remove(launchRequest.path);
        }

        await _mutex.WaitAsync();

        try
        {
            RunnerDto? selectedRunner = await GetRunnerProfile(launchRequest.runnerId);

            if (selectedRunner == null)
            {
                throw new Exception($"Couldn't find runner in the database. used id {launchRequest.runnerId ?? -1}");
            }

            if (launchRequest.gameId.HasValue && !File.Exists(launchRequest.path))
            {
                throw new Exception($"File doesn't exist - {launchRequest.path}");
            }

            if (!selectedRunner.IsValidExtension(launchRequest.path))
            {
                throw new Exception($"Invalid file for the runner - {launchRequest.path}");
            }

            LibraryManager.TryGetCachedGame(launchRequest.gameId, out GameDto? gameDto);

            await selectedRunner.SetupRunner();
            LaunchArguments launchArguments = await selectedRunner.InitRunDetails(launchRequest);
            await HandleEmbeds(launchRequest.gameId, launchArguments, selectedRunner);

            launchArguments.gameId = gameDto?.gameId;
            launchArguments.identifier = launchRequest.path;
            launchArguments.loggingLevel = gameDto?.config.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off;

            ExecuteRunRequest(launchArguments, gameDto?.getAbsoluteLogFile);

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
            await DependencyManager.OpenYesNoModal("Failed to launch game!", e.Message);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public static async Task RunSteamGame(int gameId, long appId)
    {
        LaunchArguments dat = new LaunchArguments()
        {
            gameId = gameId,
            identifier = appId.ToString(),

            command = "steam"
        };
        dat.arguments.AddLast($"steam://rungameid/{appId}");

        ExecuteRunRequest(dat, null);
    }


    public static async Task RunWineTricks(int runnerId, string process, string? subprocess)
    {
        if (IsBinaryRunning($"{runnerId}_{process}"))
        {
            await DependencyManager.OpenYesNoModal("Already running", $"{process} is already running, close before trying again");
            return;
        }

        RunnerDto runnerDto = GetRunnerProfile(runnerId);

        await runnerDto.SetupRunner();
        LaunchArguments req = await runnerDto.InitRunDetails(new LaunchRequest() { runnerId = runnerId });

        req.command = process;

        if (!string.IsNullOrEmpty(subprocess))
            req.arguments.AddFirst(subprocess);

        req.identifier = $"{runnerId}_{process}";
        req.loggingLevel = LoggingLevel.Off;

        ExecuteRunRequest(req, null);
    }




    private static async Task HandleEmbeds(int? gameId, LaunchArguments args, RunnerDto runnerDto)
    {
        ConfigProvider<RunnerDto.RunnerConfigValues> globalConfigValues = new ConfigProvider<RunnerDto.RunnerConfigValues>(runnerDto.globalRunnerValues); // new Dictionary<RunnerDto.RunnerConfigValues, string?>();
        List<IGameEmbed> embeds = new List<IGameEmbed>();

        if (gameId.HasValue)
        {
            GameDto? game = LibraryManager.TryGetCachedGame(gameId.Value);

            if (game != null)
            {
                if (game.config.GetBoolean(Game_Config.General_LocaleEmulation, false))
                    embeds.Add(new GameEmbed_Locale());
            }
        }


        if (ConfigHandler.isOnLinux)
        {
            if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_Enabled, false))
            {
                embeds.Add(new GameEmbed_Firejail());

                if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_Networking, false))
                {
                    await globalConfigValues.SaveBool(RunnerDto.RunnerConfigValues.Generic_Sandbox_BlockNetwork, true);
                }

                if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_FileSystemIsolation, false))
                {
                    await globalConfigValues.SaveBool(RunnerDto.RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true);
                }
            }
        }


        embeds = embeds.OrderBy(x => x.getPriority).ToList();

        foreach (IGameEmbed embed in embeds)
            embed.Embed(args, globalConfigValues);
    }

    public static Process ExecuteRunRequest(LaunchArguments req, string? logFile)
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

        if (!string.IsNullOrEmpty(req.identifier))
        {
            // ActiveProcess - starts the process
            activeGames.Add(req.identifier, new ActiveProcess(req.identifier, req.gameId, process, logFile));
            onGameStatusChange?.Invoke(req.identifier, true);
        }
        else
        {
            process.Start();
        }

        return process;
    }




    public static void KillProcess(string identifier)
    {
        if (activeGames.ContainsKey(identifier))
            activeGames[identifier].Dispose();
    }

    public static async void OnExitProcess(string identifier)
    {
        if (activeGames.TryGetValue(identifier, out ActiveProcess? process) && process != null)
        {
            if (LibraryManager.TryGetCachedGame(process.getGameId, out GameDto? game) && game != null)
                await game!.UpdatePlayTime(process.GetPlayMinutes());

            process?.Dispose();
            activeGames.Remove(identifier);
        }

        onGameStatusChange?.Invoke(identifier, false);
    }





    // db stuff

    public static async Task CreateProfile(string title, string path, int typeId, string version)
    {
        dbo_Runner profile = new dbo_Runner()
        {
            runnerId = -1,
            runnerName = title,
            runnerRoot = path,
            runnerType = typeId,
            runnerVersion = version
        };

        await Database_Manager.InsertItem(profile);

    }

    public static async Task RecacheRunners()
    {
        HashSet<int> existingRunners = cachedRunners.Keys.ToHashSet();
        dbo_Runner[] runnerDbs = await Database_Manager.GetItems<dbo_Runner>(SQLFilter.OrderAsc(nameof(dbo_Runner.runnerId)));

        foreach (dbo_Runner runner in runnerDbs)
        {
            if (cachedRunners.ContainsKey(runner.runnerId))
            {
                existingRunners.Remove(runner.runnerId);
                continue;
            }

            cachedRunners.Add(runner.runnerId, await GetRunnerProfile(runner));
        }

        foreach (int existing in existingRunners)
        {
            cachedRunners.Remove(existing);
        }
    }

    // use cached options once i add the default option
    public static async Task<RunnerDto> GetRunnerProfile(int? id)
    {
        dbo_Runner? runnerDb = id.HasValue
            ? await Database_Manager.GetItem<dbo_Runner>(SQLFilter.Equal(nameof(dbo_Runner.runnerId), id))
            : await Database_Manager.GetItem<dbo_Runner>(SQLFilter.OrderAsc(nameof(dbo_Runner.runnerId)));

        return await GetRunnerProfile(runnerDb);
    }

    public static async Task<RunnerDto> GetRunnerProfile(dbo_Runner? runnerDb)
    {
        if (runnerDb == null)
        {
            throw new Exception("Runner not found");
        }

        dbo_RunnerConfig[] globalConfig = await Database_Manager.GetItems<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerDb.runnerId));
        return RunnerDto.Create(runnerDb, globalConfig);
    }

    public static RunnerDto GetRunnerProfile(int id) => cachedRunners[id];
    public static RunnerDto[] GetRunnerProfiles() => cachedRunners.Values.ToArray();

    public static async Task<int> GetGameCountForRunner(int runnerId)
        => await Database_Manager.GetCount<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.runnerId), runnerId));

    public static async Task<bool> DeleteRunnerProfile(int id)
    {
        RunnerDto runner = cachedRunners[id];

        try
        {
            Directory.Delete(runner.GetRoot(), true);
        }
        catch
        {
            if (!await DependencyManager.OpenYesNoModal("Failed deletion", "do you want to proceed with the record deletion?"))
                return false;
        }

        await Database_Manager.Delete<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), id));
        await Database_Manager.Delete<dbo_Runner>(SQLFilter.Equal(nameof(dbo_Runner.runnerId), id));

        dbo_Game temp = new dbo_Game()
        {
            gameFolder = "",
            gameName = "",
            status = 0,

            runnerId = null
        };

        await Database_Manager.Update(temp, SQLFilter.Equal(nameof(dbo_Game.runnerId), id), nameof(dbo_Game.runnerId));
        await RecacheRunners();

        return true;
    }



    // data



    public struct LaunchRequest
    {
        public int? gameId;
        public int? runnerId;
        public string path;

        public ConfigProvider<Game_Config>? gameConfig;
    }


    public class LaunchArguments
    {
        public int? gameId;
        public string? identifier;
        public LoggingLevel loggingLevel;

        public List<string> whiteListedDirs = new List<string>();

        public required string command;
        public LinkedList<string> arguments = new LinkedList<string>();
        public Dictionary<string, string> environmentArguments = new Dictionary<string, string>();
    }

    private class ActiveProcess : IDisposable
    {
        private readonly int? gameId;
        private readonly string identifier;
        private readonly Process process;

        private readonly DateTime startTime;

        private StreamWriter? writer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public int? getGameId => gameId;
        public bool IsActive => !process.HasExited;

        public int GetPlayMinutes() => (int)(DateTime.UtcNow - startTime).TotalMinutes;

        public ActiveProcess(string identifier, int? gameId, Process process, string? logFile)
        {
            if (!string.IsNullOrEmpty(logFile))
            {
                writer = new StreamWriter(logFile, System.Text.Encoding.ASCII, new FileStreamOptions()
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.ReadWrite,
                    Share = FileShare.ReadWrite
                });
            }

            this.gameId = gameId;
            this.identifier = identifier;
            this.process = process;
            this.startTime = DateTime.UtcNow;

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

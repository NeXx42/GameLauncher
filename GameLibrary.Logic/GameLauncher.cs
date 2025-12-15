using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Runners;

namespace GameLibrary.Logic
{
    public static class GameLauncher
    {
        // libc (standard C library) on Linux/Unix
        private const string Libc = "libc";

        // setpgid(pid, pgid) — sets the process group of a process
        [DllImport(Libc, SetLastError = true)]
        public static extern int setpgid(int pid, int pgid);

        // killpg(pgid, sig) — sends signal to a process group
        [DllImport(Libc, SetLastError = true)]
        public static extern int killpg(int pgid, int sig);



        private static IRunner? runner;
        private static ConcurrentDictionary<int, ActiveGame> activeProcesses = new ConcurrentDictionary<int, ActiveGame>();

        public static void Init()
        {
            if (ConfigHandler.isOnLinux)
            {
                runner = new Runner_Linux();
            }
            else
            {
                runner = new Runner_Windows();
            }
        }


        public static async void LaunchGame(int gameId)
        {
            if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Launcher_Concurrency, false))
            {
                KillAllExistingProcesses();
            }

            dbo_Game? game = LibraryHandler.GetGameFromId(gameId);

            if (game == null)
                return;


            game.lastPlayed = DateTime.UtcNow;
            await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), gameId));

            try
            {
                ProcessStartInfo info = await runner!.Run(game);
                info.UseShellExecute = false;

                Process gameProcess = new Process();

                gameProcess.StartInfo = info;
                gameProcess.EnableRaisingEvents = true;

                //MainWindow.window!.UpdateActiveBanner($"Playing - {game.gameName}");

                gameProcess.Exited += (a, b) => OnGameClose(game.id, a, b);
                activeProcesses.TryAdd(gameId, new ActiveGame(gameProcess));

                if (string.IsNullOrEmpty(game.iconPath))
                {
                    //RequestOverlay(runningGame.Value, activeGame);
                }
            }
            catch (Exception e)
            {
                OnGameClose(game.id, null, null);
                //MessageBox.Show(e.Message);
            }
        }

        private static void OnGameClose(int gameId, object? obj, EventArgs args)
        {
            if (activeProcesses.TryRemove(gameId, out ActiveGame p))
            {

            }

            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    MainWindow.window!.UpdateActiveBanner();
            //});
        }

        public static void KillAllExistingProcesses()
        {
            lock (activeProcesses)
            {
                foreach (KeyValuePair<int, ActiveGame> activeProcess in activeProcesses)
                {
                    try
                    {
                        activeProcess.Value.Kill();
                    }
                    catch { }
                }

                activeProcesses.Clear();
            }
        }

        //public static void RequestOverlay(int gameId, Process? process)
        //{
        //    if (overlay != null)
        //    {
        //        overlay.Close();
        //        overlay = null;
        //    }
        //
        //    overlay = new GameOverlay();
        //    //overlay.Owner = MainWindow.window;
        //
        //    overlay.Left = 0;
        //    overlay.Top = 0;
        //    overlay.Width = SystemParameters.PrimaryScreenWidth;
        //    overlay.Height = SystemParameters.PrimaryScreenHeight;
        //    overlay.Topmost = true;
        //    overlay.ShowActivated = false;
        //
        //    overlay.Prep(gameId);
        //    overlay.Show();
        //}


        struct ActiveGame
        {
            public Process process;
            public int? groupId;

            public ActiveGame(Process p)
            {
                process = p;
                process.Start();

                if (ConfigHandler.isOnLinux)
                {
                    groupId = process.Id;
                    setpgid(groupId.Value, groupId.Value);
                }
            }

            public void Kill()
            {
                try
                {
                    if (groupId.HasValue)
                    {
                        //killpg(groupId.Value, 9); // Linux
                        process.Kill(entireProcessTree: true);
                    }
                    else
                    {
                        process.Kill(entireProcessTree: true); // Windows
                    }

                    process.WaitForExit();
                }
                catch
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch { }
                }
            }
        }
    }
}
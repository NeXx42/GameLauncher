using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Runners;

namespace GameLibrary.Logic
{
    public static class GameLauncher
    {
        public static Action<int, bool>? OnGameRunStateChange;

        private static IRunner? runner;
        private static ConcurrentDictionary<int, Runner_Game> activeProcesses = new ConcurrentDictionary<int, Runner_Game>();

        private static string getLogFolder => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "logs");


        public static void Init()
        {
            if (!Directory.Exists(getLogFolder))
                Directory.CreateDirectory(getLogFolder);

            if (ConfigHandler.isOnLinux)
            {
                runner = new Runner_Linux();
            }
            else
            {
                runner = new Runner_Windows();
            }
        }

        public static bool IsRunning(int id) => activeProcesses.ContainsKey(id);


        public static async void LaunchGame(GameDto game)
        {
            if (IsRunning(game.getGameId))
            {
                // say game is already running?
                OnGameRunStateChange?.Invoke(game.getGameId, true); // make sure ui knows at least 

                return;
            }

            if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.Launcher_Concurrency, false))
            {
                KillAllExistingProcesses();
            }

            await game.UpdateLastPlayed();

            try
            {
                ProcessStartInfo info = await runner!.Run(game);
                info.UseShellExecute = false;

                Process gameProcess = new Process();

                gameProcess.StartInfo = info;
                gameProcess.EnableRaisingEvents = true;

                //MainWindow.window!.UpdateActiveBanner($"Playing - {game.gameName}");

                OnGameRunStateChange?.Invoke(game.getGameId, true);

                gameProcess.Exited += async (a, b) => await OnGameClose(game.getGameId, a, b);
                activeProcesses.TryAdd(game.getGameId, await runner.LaunchGame(game, gameProcess, getLogFolder));


                if (string.IsNullOrEmpty(game.getGame.iconPath))
                {
                    OverlayManager.LaunchOverlay(game.getGameId);
                }
            }
            catch (Exception e)
            {
                await OnGameClose(game.getGameId, null, null);
                //MessageBox.Show(e.Message);
            }
        }

        private static async Task OnGameClose(int gameId, object? obj, EventArgs args)
        {
            OnGameRunStateChange?.Invoke(gameId, true);

            if (activeProcesses.TryRemove(gameId, out _))
            {
            }
        }

        public static void KillAllExistingProcesses()
        {
            lock (activeProcesses)
            {
                foreach (KeyValuePair<int, Runner_Game> activeProcess in activeProcesses)
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

        public static async Task<string> GetLatestLogs(int gameId)
        {
            string path = Path.Combine(getLogFolder, $"{gameId}.log");

            if (!File.Exists(path))
                return string.Empty;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
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
    }
}
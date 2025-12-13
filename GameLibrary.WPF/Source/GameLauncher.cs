using GameLibary.Components;
using GameLibary.Source.Database.Tables;
using System.Diagnostics;
using System.IO;
using System.Windows;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace GameLibary.Source
{
    public static class GameLauncher
    {
        private static Process? activeGame;
        private static GameOverlay? overlay;

        private static int? runningGame;

        public static async void LaunchGame(int gameId)
        {
            if (activeGame != null)
            {
                activeGame.Kill();
                overlay?.Close();
            }

            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if (game != null)
            {
                runningGame = game.id;
                game.lastPlayed = DateTime.UtcNow;

                await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), runningGame.Value));

                activeGame = new Process();

                try
                {
                    (string path, string args) = await GetFileToRun(game);
                    (path, args) = await SandboxGame(path, args);

                    activeGame.StartInfo.FileName = path;
                    activeGame.StartInfo.Arguments = args;

                    activeGame.EnableRaisingEvents = true;

                    MainWindow.window!.UpdateActiveBanner($"Playing - {game.gameName}");

                    activeGame.Exited += (_, __) => OnGameClose();
                    activeGame.Start();


                    if (string.IsNullOrEmpty(game.iconPath))
                    {
                        RequestOverlay(runningGame.Value, activeGame);
                    }
                }
                catch (Exception e)
                {
                    OnGameClose();
                    MessageBox.Show(e.Message);
                }

            }
        }

        private static async Task<(string path, string args)> GetFileToRun(dbo_Game game)
        {
            string realPath = await game.GetExecutableLocation();

            if (!File.Exists(realPath))
            {
                throw new Exception("Path doesnt exist - " + realPath);
            }

            if (game.useEmulator)
            {
                string emulatorPath = (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.EmulatorPath))?.value ?? "";
                return (emulatorPath, $"-run \"{realPath}\"");
            }


            return (realPath, "");
        }

        private static async Task<(string path, string args)> SandboxGame(string path, string args)
        {
            dbo_Config? sandboxieLoc = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.SandieboxLocation);
            dbo_Config? sandboxieBox = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.SandieboxBox);

            if (sandboxieBox == null || sandboxieLoc == null)
                return (path, args);

            return (sandboxieLoc.value, $"/box:{sandboxieBox.value} \"{path}\" {args}");
        }

        private static void OnGameClose()
        {
            activeGame = null;
            runningGame = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow.window!.UpdateActiveBanner();
            });
        }

        public static void RequestOverlay(int gameId, Process? process)
        {
            if (overlay != null)
            {
                overlay.Close();
                overlay = null;
            }

            overlay = new GameOverlay();
            //overlay.Owner = MainWindow.window;

            overlay.Left = 0;
            overlay.Top = 0;
            overlay.Width = SystemParameters.PrimaryScreenWidth;
            overlay.Height = SystemParameters.PrimaryScreenHeight;
            overlay.Topmost = true;
            overlay.ShowActivated = false;

            overlay.Prep(gameId);
            overlay.Show();
        }

        public static void DetachPlayingGame()
        {
            OnGameClose();
        }
    }
}

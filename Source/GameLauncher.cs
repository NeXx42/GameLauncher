using GameLibary.Components;
using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GameLibary.Source
{
    public static class GameLauncher
    {
        private static Process? activeGame;
        private static GameOverlay? overlay;

        private static int? runningGame;

        public static void Setup()
        {

        }

        public static void LaunchGame(int gameId)
        {
            if(activeGame != null)
            {
                activeGame.Kill();
                overlay.Close();
            }

            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if(game != null) 
            {
                runningGame = game.id;

                activeGame = new Process();

                try
                {
                    string realPath = game.GetRealExecutionPath;

                    if(!File.Exists(realPath))
                    {
                        throw new Exception("Path doesnt exist - " + realPath);
                    }

                    if (game.useEmulator)
                    {
                        activeGame.StartInfo.FileName = MainWindow.EmulatorLocation;
                        activeGame.StartInfo.Arguments = $"-run \"{realPath}\"";
                    }
                    else
                    {
                        activeGame.StartInfo.FileName = realPath;
                    }
                
                
                    activeGame.EnableRaisingEvents = true;

                    activeGame.Exited += OnGameClose;
                    activeGame.Start();

                    if (string.IsNullOrEmpty(game.iconPath))
                    {
                        RequestOverlay(runningGame.Value, activeGame);
                    }
                }
                catch (Exception e)
                {
                    OnGameClose(null, null);
                    MessageBox.Show(e.Message);
                }

            }
        }

        private static void OnGameClose(object? sender, EventArgs args)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                overlay?.Close();

                overlay = null;
                activeGame = null;

                runningGame = null;
            });
        }

        public static void RequestOverlay(int gameId, Process process)
        {
            if (overlay != null)
            {
                overlay.Close();
                overlay = null;
            }

            overlay ??= new GameOverlay();
            overlay.Owner = MainWindow.window;

            overlay.Prep(process, gameId);
            overlay.Show();
        }
    }
}

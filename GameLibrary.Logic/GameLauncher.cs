using System.Diagnostics;
using System.IO;
using System.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Runners;

namespace GameLibrary.Logic
{
    public static class GameLauncher
    {
        private static Process? activeGame;
        private static IRunner? runner;

        //private static GameOverlay? overlay;

        private static int? runningGame;

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
            if (activeGame != null)
            {
                activeGame.Kill();
                //overlay?.Close();
            }

            dbo_Game? game = LibraryHandler.GetGameFromId(gameId);

            if (game != null)
            {
                runningGame = game.id;
                game.lastPlayed = DateTime.UtcNow;

                await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_Game.id), runningGame.Value));


                try
                {
                    ProcessStartInfo info = await runner.Run(game);

                    activeGame = new Process();
                    activeGame.StartInfo = info;

                    activeGame.EnableRaisingEvents = true;

                    //MainWindow.window!.UpdateActiveBanner($"Playing - {game.gameName}");

                    activeGame.Exited += (_, __) => OnGameClose();
                    activeGame.Start();


                    if (string.IsNullOrEmpty(game.iconPath))
                    {
                        //RequestOverlay(runningGame.Value, activeGame);
                    }
                }
                catch (Exception e)
                {
                    OnGameClose();
                    //MessageBox.Show(e.Message);
                }

            }
        }

        private static void OnGameClose()
        {
            activeGame = null;
            runningGame = null;

            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    MainWindow.window!.UpdateActiveBanner();
            //});
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

        public static void DetachPlayingGame()
        {
            OnGameClose();
        }
    }
}

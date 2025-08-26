using GameLibary.Source;
using System.Windows;
using System.Windows.Shapes;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for GameOverlay.xaml
    /// </summary>
    public partial class GameOverlay : Window
    {
        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }



        private Process process;
        private int gameId;

        public GameOverlay()
        {
            InitializeComponent();
        }


        public void Prep(Process window, int gameId)
        {
            this.process = window;
            this.gameId = gameId;

            btn_CaptureGame.Content = window == null ? "Close" : "Game";
        }


        private void btn_CaptureGame_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            IntPtr hwnd = process?.MainWindowHandle ?? IntPtr.Zero;

            if (hwnd == IntPtr.Zero) return; // window not ready yet

            // Get the window size
            RECT rect;
            if (!GetWindowRect(hwnd, out rect)) return;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                IntPtr hdc = gfx.GetHdc();
                PrintWindow(hwnd, hdc, 0);
                gfx.ReleaseHdc(hdc);
            }

            FileManager.SaveScreenshot(bmp);
            LibaryHandler.UpdateGameIcon(gameId);
        }

        private void btn_CaptureScreen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            // Get the size of the primary screen
            System.Drawing.Rectangle bounds = new System.Drawing.Rectangle(0, 0, 1920, 1080);
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            FileManager.SaveScreenshot(bitmap);
            LibaryHandler.UpdateGameIcon(gameId);
        }
    }
}

using GameLibary.Source;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for GameOverlay.xaml
    /// </summary>
    public partial class GameOverlay : Window
    {
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



        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT rect, int cbAttribute);


        private Process? process;
        private int gameId;

        public GameOverlay()
        {
            InitializeComponent();

            btn_CaptureGame.RegisterClick(btn_CaptureGame_Click);
            btn_CaptureScreen.RegisterClick(btn_CaptureScreen_Click);
            btn_Process.RegisterClick(ChooseProcess);

            Left = 0;
            Top = 0;

            Width = 450;
            Height = 40;

            Loaded += OverlayWindow_Loaded;
        }


        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }


        public void Prep(Process? window, int gameId)
        {
            this.process = window;
            this.gameId = gameId;

            btn_Process.Label = process?.MainWindowTitle ?? "unselected";
        }

        private async void ChooseProcess()
        {
            btn_Process.Label = "selecting...";
            process = await Task.Run(WaitForSelectionOfProcess);
            btn_Process.Label = process?.MainWindowTitle ?? "unselected";
        }

        private async void btn_CaptureGame_Click()
        {
            if ((process?.MainWindowHandle ?? IntPtr.Zero) != IntPtr.Zero)
            {
                ShowWindow(process!.MainWindowHandle, SW_RESTORE);
                await Task.Delay(50);

                SetForegroundWindow(process!.MainWindowHandle);
                await Task.Delay(50);

                ScreenShot(GetBoundsForProcess(process!.MainWindowHandle));
            }
            else
            {

                ScreenShot(GetScreenBounds());
            }

            this.Close();
        }

        private async Task<Process> WaitForSelectionOfProcess()
        {
            // doesnt work properly for emulated games .... WHY
            IntPtr current = GetForegroundWindow();

            while (true)
            {
                IntPtr next = GetForegroundWindow();
                if (next != current)
                {
                    GetWindowThreadProcessId(next, out uint pid);
                    return Process.GetProcessById((int)pid);
                }

                await Task.Delay(50);
            }
        }


        private void btn_CaptureScreen_Click()
        {
            ScreenShot(GetScreenBounds());
            this.Close();
        }

        private async void ScreenShot(System.Drawing.Rectangle bounds)
        {
            this.Visibility = Visibility.Hidden;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            FileManager.SaveScreenshot(bitmap);
            await LibaryHandler.UpdateGameIcon(gameId);
        }

        private System.Drawing.Rectangle GetBoundsForProcess(nint hwnd)
        {
            RECT rect;
            int res = DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));

            if (res != 0)
            {
                if (!GetWindowRect(hwnd, out rect))
                    return GetScreenBounds();
            }

            return new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private System.Drawing.Rectangle GetScreenBounds() => new System.Drawing.Rectangle(0, 0, 1920, 1080);
    }
}

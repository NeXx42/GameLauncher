using GameLibary.Source;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for GameOverlay.xaml
    /// </summary>
    public partial class GameOverlay : Window
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_RESTORE = 9;


        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SNAPSHOT = 0x2C; // Print Screen key





        private Process?[] processList;
        private RECT? screenshotRect;

        private int? gameId;

        public GameOverlay()
        {
            InitializeComponent();

            btn_Close.RegisterClick(Close);
            btn_Refresh.RegisterClick(RefreshProcessList);
            btn_Capture.RegisterClick(CaptureScreenshot);

            RefreshProcessList();
        }

        public void Prep(int gameId)
        {
            this.gameId = gameId;
        }


        private void RefreshProcessList()
        {
            processList = [null, ..GetProcesses().ToArray()];

            inp_Process.Setup(processList.Select(x => x?.ProcessName ?? "==[ Screen ]=="), 0, SelectProcess);
            SelectProcess();
        }

        private List<Process> GetProcesses()
        {
            int sessionId = Process.GetCurrentProcess().SessionId;
            List<Process> p =new List<Process>();

            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    // Only processes in the same session as the current user
                    if (proc.SessionId != sessionId)
                        continue;

                    // Only processes with a visible main window
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                    {
                        p.Add(proc);
                        Console.WriteLine($"{proc.ProcessName} (ID: {proc.Id}) - Title: {proc.MainWindowTitle}");
                    }
                }
                catch
                {
                    // Some system processes might throw exceptions when accessing properties
                }
            }

            return p;
        }

        private void SelectProcess()
        {
            Process? p = processList[inp_Process.selectedIndex];

            if(p == null)
            {
                Higlight.Visibility = Visibility.Hidden;
                return;
            }

            Higlight.Visibility = Visibility.Visible;
            if (GetWindowRect(p.MainWindowHandle, out RECT rect))
            {
                Canvas.SetTop(Higlight, rect.Top);
                Canvas.SetLeft(Higlight, rect.Left);

                Higlight.Width = rect.Right - rect.Left;
                Higlight.Height = rect.Bottom - rect.Top;

                screenshotRect = rect;
            }

            ShowWindow(p.MainWindowHandle, SW_RESTORE);
            SetForegroundWindow(p.MainWindowHandle);
        }



        private async void CaptureScreenshot()
        {
            if(gameId == null)
            {
                Close();
                return;
            }


            this.Visibility = Visibility.Hidden;
            Higlight.Visibility = Visibility.Hidden;
            
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));

            Rectangle bounds;

            if(screenshotRect == null)
            {
                bounds = new Rectangle(0, 0, (int)Math.Round(this.Width), (int)Math.Round(this.Height));
            }
            else 
            {
                bounds = new Rectangle(screenshotRect.Value.Left, screenshotRect.Value.Top, screenshotRect.Value.Right - screenshotRect.Value.Left, screenshotRect.Value.Bottom - screenshotRect.Value.Top);
            }

            await Screenshot_GDI(bounds, SaveResult);
            //await Screenshot_PrntScreen(bounds, SaveResult);

            Close();

            async Task SaveResult(Bitmap bitmap)
            {
                FileManager.SaveScreenshot(bitmap);
                await LibaryHandler.UpdateGameIcon(gameId.Value);
            }
        }

        private async Task Screenshot_GDI(Rectangle bounds, Func<Bitmap, Task> saveFunc)
        {
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                await saveFunc(bitmap);
            }
        }

        private async Task Screenshot_PrntScreen(Rectangle bounds, Func<Bitmap, Task> saveFunc)
        {
            IDataObject oldObj = Clipboard.GetDataObject();

            keybd_event(VK_SNAPSHOT, 0, 0, UIntPtr.Zero);           // key down
            keybd_event(VK_SNAPSHOT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // key up

            BitmapSource screenshot = null;

            for (int i = 0; i < 10; i++)
            {
                if (Clipboard.ContainsImage())
                {
                    screenshot = Clipboard.GetImage();
                    break;
                }
                await Task.Delay(50);
            }

            if (screenshot != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(screenshot));
                    encoder.Save(ms);

                    using var bmp = new Bitmap(ms);
                    using var cropped = bmp.Clone(bounds, bmp.PixelFormat);

                    await saveFunc(cropped);
                }
            }

            Clipboard.SetDataObject(oldObj);
        }
    }
}

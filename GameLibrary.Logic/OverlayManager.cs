using System.Diagnostics;

namespace GameLibrary.Logic;

public static class OverlayManager
{
    public const string GTK_OVERLAY_RETURN_IDENTIFIER = "GTKOVERLAY_RETURNPATH:";

    public delegate Task OpenOverlayRequest(int gameId);

    private static OpenOverlayRequest? openRequest;
    private static (Thread thread, Process process)? activeGTKOverlay;

    public static void Init(OpenOverlayRequest openRequest)
    {
        OverlayManager.openRequest = openRequest;
    }

    public static async Task LaunchOverlay(int gameId)
    {
        if (ConfigHandler.isOnLinux)
        {
            if (activeGTKOverlay.HasValue)
            {
                activeGTKOverlay.Value.process.Kill();
                await activeGTKOverlay.Value.process.WaitForExitAsync();
            }

            string gtkOverlay = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameLibrary_GTKOverlay");

            if (!File.Exists(gtkOverlay))
                throw new Exception("GTK overlay not found");

            var processInfo = new ProcessStartInfo
            {
                FileName = gtkOverlay,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Process p = Process.Start(processInfo)!;

            activeGTKOverlay = (new Thread(() => ListenForGTKOutput(gameId, p)), p);
            activeGTKOverlay.Value.thread.Start();
        }
        else
        {
            openRequest?.Invoke(gameId);
        }
    }

    private static async Task ListenForGTKOutput(int gameId, Process p)
    {
        Console.WriteLine("start listen");
        while (!p.HasExited)
        {
            string? line = await p.StandardOutput.ReadLineAsync();
            Console.WriteLine(line);

            if (line?.StartsWith(GTK_OVERLAY_RETURN_IDENTIFIER) ?? false)
            {
                string path = line.Remove(0, GTK_OVERLAY_RETURN_IDENTIFIER.Length);
                await FileManager.UpdateGameIcon(gameId, new Uri(path));

                p.Kill();
                break;
            }
        }

        activeGTKOverlay = null;
    }
}

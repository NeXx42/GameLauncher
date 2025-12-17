using System.Diagnostics;
using System.Text;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.Runners;

public interface IRunner
{
    public Task<ProcessStartInfo> Run(GameDto game);
    public Task<Runner_Game> LaunchGame(Process startInfo, string logPath);
}


public abstract class Runner_Game
{
    public Process process;
    public int? groupId;

    private StreamWriter? logOutput;
    private Thread loggingThread;

    public Runner_Game(string logPath, Process p)
    {
        if (!string.IsNullOrEmpty(logPath))
        {
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            loggingThread = new Thread(async () => await LoggingThread(logPath));
        }

        process = p;
        process.Start();
        loggingThread?.Start();

        PostRun();
    }

    public virtual void PostRun() { }


    private async Task LoggingThread(string path)
    {
        using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
        using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    writer.WriteLine("OUT: " + e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    writer.WriteLine("ERR: " + e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(process.WaitForExit);
            await writer.FlushAsync();
        }
    }


    public async Task Kill()
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

    ~Runner_Game()
    {
        logOutput?.Close();
        logOutput = null;
    }
}

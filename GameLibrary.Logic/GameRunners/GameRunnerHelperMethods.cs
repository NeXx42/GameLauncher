using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.GameRunners;

public static class GameRunnerHelperMethods
{
    public static string GetRoot(this dbo_Runner runner)
        => Path.Combine(runner.runnerRoot, runner.runnerName.Replace(" ", string.Empty));

    public static void EnsureDirectoryExists(string where)
    {
        if (!Directory.Exists(where))
            Directory.CreateDirectory(where);
    }


    public static async Task RunBasicProcess(string path, params string[] args)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = path;

        foreach (string arg in args)
            info.ArgumentList.Add(arg);

        Process p = new Process();
        p.StartInfo = info;

        p.Start();
        await p.WaitForExitAsync();
    }

    public static bool IsValidExtension(string path, IGameRunner runner)
    {
        string[] allowed = runner.getAcceptableExtensions;

        foreach (string extension in allowed)
            if (!path.EndsWith($".{extension}"))
                return false;

        return true;
    }

    public static bool TryGetConfig(this Dictionary<string, string?> inp, RunnerManager.RunnerConfigValues config, out string res)
    {
        if (inp.TryGetValue(config.ToString(), out string? _res) && !string.IsNullOrEmpty(_res))
        {
            res = _res;
            return true;
        }

        res = string.Empty;
        return false;
    }

    public static bool HasConfigValueOf(this Dictionary<string, string?> inp, RunnerManager.RunnerConfigValues config, bool trueValue)
        => inp.HasConfigValueOf(config, trueValue ? "1" : "0");

    public static bool HasConfigValueOf(this Dictionary<string, string?> inp, RunnerManager.RunnerConfigValues config, string trueValue)
    {
        if (inp.TryGetConfig(config, out string res))
            return res.Equals(trueValue);

        return false;
    }

    public static void AddOrOverride(this Dictionary<string, string?> inp, RunnerManager.RunnerConfigValues config, bool to)
        => inp.AddOrOverride(config, to ? "1" : "0");

    public static void AddOrOverride(this Dictionary<string, string?> inp, RunnerManager.RunnerConfigValues config, string to)
    {
        inp[config.ToString()] = to;
    }
}

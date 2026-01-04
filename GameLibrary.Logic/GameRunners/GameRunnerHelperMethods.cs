using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameRunners;

public static class GameRunnerHelperMethods
{
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

    public static bool TryGetConfig(this Dictionary<RunnerDto.RunnerConfigValues, string?> inp, RunnerDto.RunnerConfigValues config, out string res)
    {
        if (inp.TryGetValue(config, out string? _res) && !string.IsNullOrEmpty(_res))
        {
            res = _res;
            return true;
        }

        res = string.Empty;
        return false;
    }

    public static bool HasConfigValueOf(this Dictionary<RunnerDto.RunnerConfigValues, string?> inp, RunnerDto.RunnerConfigValues config, bool trueValue)
        => inp.HasConfigValueOf(config, trueValue ? "1" : "0");

    public static bool HasConfigValueOf(this Dictionary<RunnerDto.RunnerConfigValues, string?> inp, RunnerDto.RunnerConfigValues config, string trueValue)
    {
        if (inp.TryGetConfig(config, out string res))
            return res.Equals(trueValue);

        return false;
    }

    public static void AddOrOverride(this Dictionary<RunnerDto.RunnerConfigValues, string?> inp, RunnerDto.RunnerConfigValues config, bool to)
        => inp.AddOrOverride(config, to ? "1" : "0");

    public static void AddOrOverride(this Dictionary<RunnerDto.RunnerConfigValues, string?> inp, RunnerDto.RunnerConfigValues config, string to)
    {
        inp[config] = to;
    }
}

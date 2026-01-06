using System.Diagnostics;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_WineGE : RunnerDto_Wine
{
    private const string GITHUB_NAME = "GloriousEggroll/wine-ge-custom";

    protected readonly string version;
    protected readonly string binaryFolder;

    protected string? binaryPath;

    protected string getWineLib => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "lib");
    protected override string getWineExecutable => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "bin", "wine64");


    public RunnerDto_WineGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        version = runner.runnerVersion;
        binaryFolder = Path.Combine(rootLoc, "binaries", version);
    }


    public static new async Task<string[]?> GetRunnerVersions()
    {
        return (await GetVersionData()).OrderByDescending(x => x.id).Select(x => x.json.GetProperty("tag_name").GetString()).ToArray()!;
    }

    private static async Task<(int id, JsonElement json)[]> GetVersionData()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "test");
            HttpResponseMessage res = await client.GetAsync($"https://api.github.com/repos/{GITHUB_NAME}/releases");

            var json = await res.Content.ReadAsStringAsync();
            JsonDocument doc = JsonDocument.Parse(json);

            List<(int id, JsonElement)> versions = new List<(int id, JsonElement)>();

            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                int version = el.GetProperty("id").GetInt32();
                versions.Add((version, el));
            }

            return versions.ToArray();
        }
    }

    public override async Task SetupRunner()
    {
        if (!Directory.Exists(binaryFolder))
        {
            await InstallWine();
        }
    }

    protected async Task InstallWine()
    {
        int? selectedVersion = null;
        (int, JsonElement json)[] releases = await GetVersionData();

        for (int i = 0; i < releases.Length; i++)
        {
            if (releases[i].json.GetProperty("tag_name").GetString() == version)
            {
                selectedVersion = i;
                break;
            }
        }

        if (selectedVersion == null)
            throw new Exception("Couldn't match version with tag");

        foreach (JsonElement asset in releases[selectedVersion.Value].json.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("content_type").GetString() == "application/x-xz")
            {
                string url = asset.GetProperty("browser_download_url").GetString()!;

                await DownloadFile(url, $"{binaryFolder}.tar.xz");
                await ExtractFile($"{binaryFolder}.tar.xz", binaryFolder);

                File.Delete($"{binaryFolder}.tar.xz");

                //await RunWine("wineboot");

                return;
            }
        }

        throw new Exception("Failed to find asset to download");
    }

    private async Task DownloadFile(string url, string outputFile)
    {
        if (!File.Exists(outputFile)) // maybe clear
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            res.EnsureSuccessStatusCode();


            using (Stream contentStream = await res.Content.ReadAsStreamAsync())
            using (FileStream filestream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(filestream);
            }
        }
    }

    private async Task ExtractFile(string extract, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xf {extract} -C {outputFolder}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        Process p = new Process();
        p.StartInfo = startInfo;

        p.Start();
        await p.WaitForExitAsync();
    }


    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        var res = await base.InitRunDetails(game);
        res.whiteListedDirs.Add(binaryFolder);

        res.environmentArguments.Add("LD_LIBRARY_PATH", getWineLib);
        return res;
    }

    protected override void AddLogging(RunnerManager.LaunchArguments args, LoggingLevel level)
    {
        base.AddLogging(args, level);
        
        switch (level)
        {
            case LoggingLevel.High:
            case LoggingLevel.Everything:
                args.environmentArguments.Add("DXVK_LOG_LEVEL", "info");
                break;
        }
    }
}

using System.Diagnostics;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.GameRunners;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_WineGE : RunnerDto_Wine
{
    private const string GITHUB_NAME = "GloriousEggroll/wine-ge-custom";

    protected readonly string version;
    protected readonly string binaryFolder;
    protected readonly string dxvkFolder;

    protected string? binaryPath;

    protected string getWineLib => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "lib");
    protected override string getWineExecutable => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "bin", "wine64");


    public RunnerDto_WineGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        version = runner.runnerVersion;

        GameRunnerHelperMethods.EnsureDirectoryExists(Path.Combine(rootLoc, "DXVK"));

        dxvkFolder = Path.Combine(rootLoc, "DXVK", "latest"); // add versioning later
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

        //if (!Directory.Exists(dxvkFolder))
        //{
        //    await InstallDXVK();
        //}
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

    protected async Task InstallDXVK()
    {

        await DownloadFile("https://github.com/doitsujin/dxvk/releases/download/v2.7.1/dxvk-2.7.1.tar.gz", $"{dxvkFolder}.tar.xz");
        await ExtractFile($"{dxvkFolder}.tar.xz", dxvkFolder);

        string root = Directory.GetDirectories(dxvkFolder).First();

        CopyDLLS(Path.Combine(root, "x64"), Path.Combine(prefixFolder, "drive_c", "windows", "system32"));
        CopyDLLS(Path.Combine(root, "x32"), Path.Combine(prefixFolder, "drive_c", "windows", "syswow64"));

        await RunWine("reg", "add", @"HKCU\\Software\\Wine\\Direct3D\", "/v", "EnableDXVK", "/t", "REG_DWORD", "/d", "1", "/f");

        void CopyDLLS(string from, string to)
        {
            string[] libraries = Directory.GetFileSystemEntries(from);

            foreach (string library in libraries)
            {
                string destName = Path.GetFileName(library);
                File.Copy(library, Path.Combine(to, destName), true);
            }
        }
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

        return res;
    }

    protected override Dictionary<string, string> GetWineEnvironmentVariables()
    {
        var defaultVars = base.GetWineEnvironmentVariables();
        defaultVars.Add("DXVK_LOG_LEVEL=info", "info");
        defaultVars.Add("LD_LIBRARY_PATH", getWineLib);

        return defaultVars;
    }
}

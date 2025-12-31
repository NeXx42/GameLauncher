using System.Diagnostics;
using System.Net;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.GameRunners;

public class GameRunner_WineGE : GameRunner_Wine
{
    private const string GITHUB_NAME = "GloriousEggroll/wine-ge-custom";

    public static new async Task<string[]?> GetRunnerVersions()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "test");
            HttpResponseMessage res = await client.GetAsync($"https://api.github.com/repos/{GITHUB_NAME}/releases");

            var json = await res.Content.ReadAsStringAsync();
            JsonDocument doc = JsonDocument.Parse(json);

            return ["5.12"];
        }
    }

    public GameRunner_WineGE(dbo_Runner data) : base(data)
    {
    }

    protected override async Task InstallWine()
    {
        await DownloadFile($"https://github.com/{GITHUB_NAME}/releases/download/GE-Proton8-26/wine-lutris-GE-Proton8-26-x86_64.tar.xz", $"{binaryFolder}.tar.xz");
        await ExtractFile($"{binaryFolder}.tar.xz", binaryFolder);

        await RunWine("wineboot");
    }

    protected override async Task InstallDXVK()
    {
        return;

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
}

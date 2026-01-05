using CSharpSqliteORM;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.Logic;

public static class DependencyManager
{
    private static IUILinker? uiLinker;

    public const string APPLICATION_NAME = "MyLibraryApplication";
    public const string DB_POINTER_FILE = "dblink";

    public static string GetUserStorageFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME);
    public static string? cachedDBLocation { get; private set; }


    public static async Task PreSetup(IUILinker linker, IImageFetcher imageFetcher)
    {
        uiLinker = linker;
        ImageManager.Init(imageFetcher);

        string root = GetUserStorageFolder();

        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        string dbPointerFile = Path.Combine(root, DB_POINTER_FILE);

        if (File.Exists(dbPointerFile))
        {
            string pointer = File.ReadAllText(dbPointerFile);

            if (File.Exists(pointer))
                cachedDBLocation = pointer;
        }
    }

    public static async Task CreateDBPointerFile(string path)
    {
        string dbPointerFile = Path.Combine(GetUserStorageFolder(), DB_POINTER_FILE);

        if (File.Exists(dbPointerFile))
            File.Delete(dbPointerFile);

        await File.WriteAllTextAsync(dbPointerFile, path);
        cachedDBLocation = path;
    }

    public static async Task LoadDatabase(string? newPointerFile = null)
    {
        List<Func<Task>> toRun = new List<Func<Task>>();

        if (!string.IsNullOrEmpty(newPointerFile))
        {
            toRun.Add(() => CreateDBPointerFile(newPointerFile));
        }

        if (string.IsNullOrEmpty(cachedDBLocation))
        {
            throw new Exception("Invalid pointer file");
        }

        await Database_Manager.Init(cachedDBLocation, HandleDatabaseException);

        foreach (var task in toRun)
        {
            await task();
        }
    }

    private static async void HandleDatabaseException(Exception error, string? sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            await OpenYesNoModal("Database Logic Exception", $"{error.Message}\n\n{error.StackTrace}");
        }
        else
        {
            await OpenYesNoModal("SQL Exception", $"{sql}\n{error.Message}");
        }
    }

    public static async Task PostSetup()
    {
        await OpenLoadingModal(true,
            RunnerManager.Init,
            LibraryHandler.Setup,
            ConfigHandler.Init,
            TagManager.Init
        );
    }

    public static void InvokeOnUIThread(Action a)
        => uiLinker!.InvokeOnUIThread(a);

    public static void InvokeOnUIThread(Func<Task> a)
        => uiLinker!.InvokeOnUIThread(async () => await a());

    public static async Task OpenLoadingModal(bool progressiveLoad, params Func<Task>[] tasks)
        => await uiLinker!.OpenLoadingModal(progressiveLoad, tasks);

    public static async Task<string?> OpenStringInputModal(string title, string? existingText = "")
        => await uiLinker!.OpenStringInputModal(title, existingText);

    public static async Task<bool> OpenYesNoModal(string title, string paragraph)
        => await uiLinker!.OpenYesNoModal(title, paragraph);

    public static async Task<bool> OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string loadingMessage)
        => await uiLinker!.OpenYesNoModalAsync(title, paragraph, positiveCallback, loadingMessage);

    public static async Task<int> OpenConfirmationAsync(string title, string paragraph, params (string btn, Func<Task> callback, string? loadingMessage)[] controls)
        => await uiLinker!.OpenConfirmationAsync(title, paragraph, controls);
}

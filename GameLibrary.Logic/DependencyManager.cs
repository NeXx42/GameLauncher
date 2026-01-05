using GameLibrary.Logic.Interfaces;

namespace GameLibrary.Logic;

public static class DependencyManager
{
    private static IUILinker? uiLinker;


    public static async Task PreSetup(IUILinker linker)
    {
        uiLinker = linker;
        await DatabaseManager.Init();
    }

    public static async Task LoadDatabase(string? newPointerFile = null)
    {
        List<Func<Task>> toRun = new List<Func<Task>>();

        if (!string.IsNullOrEmpty(newPointerFile))
        {
            toRun.Add(() => DatabaseManager.CreateDBPointerFile(newPointerFile));
        }

        toRun.Add(() => DatabaseManager.LoadDatabase());

        foreach (var task in toRun)
        {
            await task();
        }
    }


    public static async Task PostSetup(IImageFetcher imageFetcher)
    {
        await RunnerManager.Init();
        await LibraryHandler.Setup();
        await ConfigHandler.Init();
        await TagManager.Init();

        ImageManager.Init(imageFetcher);
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

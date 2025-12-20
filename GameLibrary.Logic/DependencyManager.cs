using GameLibrary.Logic.Interfaces;

namespace GameLibrary.Logic;

public static class DependencyManager
{
    public static IUILinker? uiLinker { private set; get; }


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

        toRun.Add(() => DatabaseManager.LoadDatabase(null));

        foreach (var task in toRun)
        {
            await task();
        }
    }


    public static async Task PostSetup(IImageFetcher imageFetcher)
    {
        await LibraryHandler.Setup();
        await ConfigHandler.Init();

        OverlayManager.Init(null);
        ImageManager.Init(imageFetcher);
        GameLauncher.Init();
    }
}

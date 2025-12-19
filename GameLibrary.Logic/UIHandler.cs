namespace GameLibrary.Logic;

public static class UIHandler
{
    private static UIRequests requests;

    public static void Register(UIRequests requests)
    {
        UIHandler.requests = requests;
    }

    public static async Task LoadTask(bool showLoading, params Func<Task>[] t)
    {
        await requests.loader(showLoading, t);
    }

    public struct UIRequests
    {
        public Func<bool, Func<Task>[], Task> loader;
    }
}

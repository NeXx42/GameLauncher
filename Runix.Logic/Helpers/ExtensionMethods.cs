using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.Helpers;

public static class ExtensionMethods
{
    public static IEnumerable<GameDto> Filter_Tags(this IEnumerable<GameDto> inp, HashSet<int> tagFilter)
    {
        if (tagFilter?.Count <= 0)
            return inp;

        return inp.Where(x => x.IsInFilter(ref tagFilter!));
    }

    public static IEnumerable<GameDto> Filter_Text(this IEnumerable<GameDto> inp, string? textFilter)
    {
        if (string.IsNullOrEmpty(textFilter))
            return inp;

        return inp.Where(x => x.gameName.StartsWith(textFilter, StringComparison.InvariantCultureIgnoreCase));
    }

    public static IEnumerable<GameDto> Filter_OrderType(this IEnumerable<GameDto> inp, GameFilterRequest.OrderType orderType)
    {
        switch (orderType)
        {
            case GameFilterRequest.OrderType.Id: return inp.OrderBy(x => x.gameId);
            case GameFilterRequest.OrderType.Name: return inp.OrderBy(x => x.gameName);
            case GameFilterRequest.OrderType.LastPlayed: return inp.OrderBy(x => x.lastPlayed);
        }

        return inp;
    }

    public static IEnumerable<GameDto> Filter_Direction(this IEnumerable<GameDto> inp, bool isAsc)
    {
        return isAsc ? inp : inp.Reverse();
    }

    public static string CreateDirectoryIfNotExists(this string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }


    public static void RegisterTask(this Action? act, Func<Task>? callback) { if (callback != null) act += () => _ = callback?.Invoke(); }
    public static void RegisterTask<T>(this Action<T>? act, Func<T, Task>? callback) { if (callback != null) act += (a) => _ = callback?.Invoke(a); }
    public static void RegisterTask<T, T2>(this Action<T, T2>? act, Func<T, T2, Task>? callback) { if (callback != null) act += (a, b) => _ = callback?.Invoke(a, b); }
}

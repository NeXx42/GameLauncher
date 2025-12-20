using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.Helpers;

public static class ExtensionMethods
{
    public static IEnumerable<GameDto> Filter_Tags(this IEnumerable<GameDto> inp, HashSet<int> tagFilter)
    {
        if (tagFilter?.Count <= 0)
            return inp;

        return inp.Where(x => x.IsInFilter(ref tagFilter));
    }

    public static IEnumerable<GameDto> Filter_Text(this IEnumerable<GameDto> inp, string? textFilter)
    {
        if (string.IsNullOrEmpty(textFilter))
            return inp;

        return inp.Where(x => x.getGame.gameName.StartsWith(textFilter, StringComparison.InvariantCultureIgnoreCase));
    }

    public static IEnumerable<GameDto> Filter_OrderType(this IEnumerable<GameDto> inp, LibraryHandler.OrderType orderType)
    {
        switch (orderType)
        {
            case LibraryHandler.OrderType.Id: return inp.OrderBy(x => x.getGameId);
            case LibraryHandler.OrderType.Name: return inp.OrderBy(x => x.getGame.gameName);
            case LibraryHandler.OrderType.LastPlayed: return inp.OrderBy(x => x.getGame.lastPlayed);
        }

        return inp;
    }

    public static IEnumerable<GameDto> Filter_Direction(this IEnumerable<GameDto> inp, bool isAsc)
    {
        return isAsc ? inp : inp.Reverse();
    }
}

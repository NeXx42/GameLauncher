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
}

using System.Diagnostics;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Runners;

public interface IRunner
{
    public Task<ProcessStartInfo> Run(dbo_Game game);
}

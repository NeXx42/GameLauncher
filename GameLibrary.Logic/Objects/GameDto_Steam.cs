using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects;

public class GameDto_Steam : GameDto
{
    public GameDto_Steam(dbo_Game game, dbo_GameTag[] tags) : base(game, tags)
    {
    }

    public override Task Launch()
    {
        throw new NotImplementedException();
    }
}

using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;

namespace GameLibrary.Logic.Objects.Tags;

public class TagDto_Managed : TagDto
{
    public enum ManagedTagType
    {
        Steam = -1
    }

    private ManagedTagType type;

    public TagDto_Managed(ManagedTagType type) : base(new dbo_Tag()
    {
        TagName = type.ToString(),
        TagId = (int)type
    })
    {
        this.type = type;
    }

    public override bool canToggle => false;

    public bool DoesFitGame(GameDto game)
    {
        switch (type)
        {
            case ManagedTagType.Steam: return game is GameDto_Steam;
        }


        return false;
    }

    public void HandleFilterSQL(ref List<string> joinClause, ref List<string> whereClause, ref List<string> groupClause, ref List<string> havingClause)
    {
        switch (type)
        {
            case ManagedTagType.Steam:
                joinClause.Add($"INNER JOIN {dbo_Libraries.tableName} tls ON g.{nameof(dbo_Game.libraryId)} = tls.{nameof(dbo_Libraries.libaryId)}");
                whereClause.Add($"tls.{nameof(dbo_Libraries.libraryExternalType)} = {(int)Library_ExternalProviders.Steam}");
                break;
        }
    }
}

using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects.Tags;

public class TagDto
{
    public readonly int id;
    public readonly string name;

    public TagDto(dbo_Tag tag)
    {
        id = tag.TagId;
        name = tag.TagName;
    }

    public virtual bool canToggle => true;
}

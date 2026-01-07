using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects.Tags;

public class TagDto
{
    public readonly int id;
    public string name;

    public TagDto(dbo_Tag tag)
    {
        id = tag.TagId;
        name = tag.TagName;
    }

    public virtual bool canToggle => true;

    public async Task UpdateName(string to)
    {
        name = to;

        dbo_Tag dto = new dbo_Tag()
        {
            TagId = id,
            TagName = to
        };

        await Database_Manager.Update(dto, SQLFilter.Equal(nameof(dbo_Tag.TagId), id));
    }

    public async Task Delete()
    {
        await Database_Manager.Delete<dbo_Tag>(SQLFilter.Equal(nameof(dbo_Tag.TagId), id));
        await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.TagId), id));
    }
}

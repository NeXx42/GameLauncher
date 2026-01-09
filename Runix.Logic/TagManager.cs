using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.Logic;

public static class TagManager
{
    private static List<TagDto> managedTags = new List<TagDto>();
    private static Dictionary<int, TagDto> unmanagedTags = new Dictionary<int, TagDto>();

    public static Action<TagDto?, bool>? onTagChange;

    public static bool getAreTagsDirty
    {
        get
        {
            if (m_AreTagsDirty)
            {
                m_AreTagsDirty = false;
                return true;
            }

            return false;
        }
    }
    private static bool m_AreTagsDirty;

    public static async Task Init()
    {
        await GenerateManagedTags();
        await LoadUnmanagedTags();
    }

    private static async Task GenerateManagedTags()
    {
        managedTags = new List<TagDto>();

        if (await Database_Manager.Exists<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libraryExternalType), Library_ExternalProviders.Steam)))
        {
            managedTags.Add(new TagDto_Managed(TagDto_Managed.ManagedTagType.Steam));
        }
    }

    private static async Task LoadUnmanagedTags()
    {
        HashSet<int> existingTags = unmanagedTags.Keys.ToHashSet();
        int[] tags = await Database_Manager.GetItemsGeneric($"SELECT {nameof(dbo_Tag.TagId)} id FROM {dbo_Tag.tableName}", (reader) => Task.FromResult(int.Parse(reader["id"].ToString()!)));

        // improvement would be to do a single database call with the missing tags instead of individually. too much work
        foreach (int tag in tags)
        {
            if (!unmanagedTags.ContainsKey(tag))
            {
                dbo_Tag? dto = await Database_Manager.GetItem<dbo_Tag>(SQLFilter.Equal(nameof(dbo_Tag.TagId), tag));
                unmanagedTags.Add(tag, new TagDto(dto!));
            }

            existingTags.Remove(tag);
        }

        foreach (int oldTag in existingTags)
        {
            unmanagedTags.Remove(oldTag);
        }
    }

    public static async Task<TagDto[]> GetAllTags()
    {
        if (m_AreTagsDirty)
        {
            await LoadUnmanagedTags();
        }

        return managedTags.Union(unmanagedTags.Values).ToArray();
    }

    public static TagDto[] GetTagsForAGame(GameDto game)
    {
        List<TagDto> tags = new List<TagDto>();

        foreach (int tagId in game.tags)
        {
            if (unmanagedTags.TryGetValue(tagId, out TagDto? tag) && tag != null)
                tags.Add(tag);
        }

        foreach (TagDto_Managed managedTag in managedTags)
        {
            if (managedTag.DoesFitGame(game))
                tags.Add(managedTag);
        }

        return tags.ToArray();
    }

    public static TagDto? GetTagById(int? id)
    {
        if (!id.HasValue)
            return null;

        if (unmanagedTags.TryGetValue(id.Value, out TagDto? tag))
            return tag;

        return null;
    }

    public static async Task AddOrUpdateTag(int? id, string tag)
    {
        if (id.HasValue)
        {
            await unmanagedTags[id.Value].UpdateName(tag);
            onTagChange?.Invoke(unmanagedTags[id.Value], false);

            return;
        }

        dbo_Tag dto = new dbo_Tag() { TagName = tag };
        await Database_Manager.InsertItem(dto);

        MarkTagsAsDirty();
        onTagChange?.Invoke(null, false);
    }

    public static async Task DeleteTag(int tagId)
    {
        if (unmanagedTags.TryGetValue(tagId, out TagDto? tag))
        {
            await tag.Delete();
            unmanagedTags.Remove(tagId);

            onTagChange?.Invoke(tag, true);
        }
    }

    public static void MarkTagsAsDirty() => m_AreTagsDirty = true;
}

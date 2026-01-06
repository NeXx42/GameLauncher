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
        dbo_Tag[] tags = await Database_Manager.GetItems<dbo_Tag>();
        unmanagedTags = tags.ToDictionary(x => x.TagId, x => new TagDto(x));
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
        List<TagDto> tags = game.tags.Select(x => unmanagedTags[x]).ToList();

        foreach (TagDto_Managed managedTag in managedTags)
        {
            if (managedTag.DoesFitGame(game))
                tags.Add(managedTag);
        }

        return tags.ToArray();
    }

    public static void MarkTagsAsDirty() => m_AreTagsDirty = true;
}

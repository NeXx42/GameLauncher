using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.Logic;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.SubPage;

public partial class Popup_TagManager : UserControl
{
    private int? currentlySelectedTag
    {
        get => m_currentlySelectedTag;
        set
        {
            if (m_currentlySelectedTag.HasValue)
            {
                unmanagedTagUIList[m_currentlySelectedTag.Value].Toggle(false);
            }

            value = value == m_currentlySelectedTag ? null : value;
            m_currentlySelectedTag = value;

            if (m_currentlySelectedTag.HasValue)
            {
                unmanagedTagUIList[m_currentlySelectedTag.Value].Toggle(true);
            }
        }
    }
    private int? m_currentlySelectedTag;

    private Dictionary<int, Library_Tag> unmanagedTagUIList;

    public Popup_TagManager()
    {
        InitializeComponent();

        unmanagedTagUIList = new Dictionary<int, Library_Tag>();

        btn_Delete.RegisterClick(DeleteTag);
        btn_Edit.RegisterClick(EditTag);

        TagManager.onTagChange += (__, ___) => _ = RedrawTagList();
    }

    public async Task OnOpen()
    {
        await SelectTag(null);
        await RedrawTagList();
    }

    private async Task RedrawTagList()
    {
        TagDto[] tags = await TagManager.GetAllTags();

        cont_ManagedTags.Children.Clear();
        cont_UnmanagedTags.Children.Clear();

        unmanagedTagUIList.Clear();

        foreach (TagDto tag in tags)
        {
            Library_Tag ui = new Library_Tag();

            if (tag is TagDto_Managed)
            {
                ui.Draw(tag, null);
                cont_ManagedTags.Children.Add(ui);
            }
            else
            {
                ui.Draw(tag, SelectTag);
                cont_UnmanagedTags.Children.Add(ui);
                unmanagedTagUIList.Add(tag.id, ui);
            }
        }
    }

    private Task SelectTag(TagDto? tag)
    {
        currentlySelectedTag = tag?.id;

        btn_Delete.IsVisible = currentlySelectedTag.HasValue;
        btn_Edit.Label = currentlySelectedTag.HasValue ? "Edit" : "Add";

        return Task.CompletedTask;
    }

    private async Task DeleteTag()
    {
        if (!currentlySelectedTag.HasValue)
            return;

        if (!await DependencyManager.OpenYesNoModal("Delete Tag?", "Are you sure you want to delete the tag?"))
            return;

        int temp = currentlySelectedTag.Value;

        await SelectTag(null);
        await TagManager.DeleteTag(temp);
    }

    private async Task EditTag()
    {
        string? existing = TagManager.GetTagById(currentlySelectedTag)?.name;
        string? res = await DependencyManager.OpenStringInputModal("Tag Name", existing);

        if (string.IsNullOrEmpty(res))
            return;

        await TagManager.AddOrUpdateTag(currentlySelectedTag, res!);
    }
}
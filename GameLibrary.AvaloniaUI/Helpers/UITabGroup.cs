using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GameLibrary.AvaloniaUI.Helpers;

public class UITabGroup
{
    protected UITabGroup_Group[] groups;

    protected int? selectedGroup;

    public UITabGroup(Panel btns, Panel content)
    {
        groups = new UITabGroup_Group[Math.Min(btns.Children.Count, content.Children.Count)];

        for (int i = 0; i < groups.Length; i++)
        {
            groups[i] = new UITabGroup_Group(content.Children[i], (Border)btns.Children[i]);
            groups[i].Setup(this, i);
        }

        for (int i = groups.Length; i < btns.Children.Count; i++)
            btns.Children[i].IsVisible = false;

        for (int i = groups.Length; i < content.Children.Count; i++)
            content.Children[i].IsVisible = false;
    }

    public UITabGroup(params UITabGroup_Group[] groups)
    {
        this.groups = groups;

        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].Setup(this, i);
        }
    }

    public virtual async Task ChangeSelection(int to)
    {
        if (selectedGroup == to)
            return;

        if (selectedGroup.HasValue)
            await groups[selectedGroup.Value].Close();

        selectedGroup = to;
        await groups[selectedGroup.Value].Open();
    }
}

public class UITabGroup_Group
{
    protected readonly Control element;
    protected readonly Control btn;

    public UITabGroup_Group(Control element, Control btn)
    {
        this.element = element;
        this.btn = btn;
    }

    public virtual void Setup(UITabGroup master, int index)
    {
        btn.PointerPressed += async (_, __) => await master.ChangeSelection(index);
        element.IsVisible = false;
    }

    public virtual Task Close()
    {
        element.IsVisible = false;
        return Task.CompletedTask;
    }

    public virtual Task Open()
    {
        element.IsVisible = true;
        return Task.CompletedTask;
    }
}
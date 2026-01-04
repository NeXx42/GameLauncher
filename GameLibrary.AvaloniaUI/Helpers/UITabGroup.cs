using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using GameLibrary.AvaloniaUI.Controls;

namespace GameLibrary.AvaloniaUI.Helpers;

public class UITabGroup
{
    protected UITabGroup_Group[] groups;

    protected int? selectedGroup;

    public UITabGroup(Panel btns, Panel content, bool useToggleButtons)
    {
        if (useToggleButtons)
        {
            groups = new UITabGroup_GroupToggleButton[Math.Min(btns.Children.Count, content.Children.Count)];

            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = new UITabGroup_GroupToggleButton(content.Children[i], (Common_ButtonToggle)btns.Children[i]);
                groups[i].Setup(this, i);
            }
        }
        else
        {
            groups = new UITabGroup_GroupToggleButton[Math.Min(btns.Children.Count, content.Children.Count)];

            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = new UITabGroup_GroupToggleButton(content.Children[i], (Common_ButtonToggle)btns.Children[i]);
                groups[i].Setup(this, i);
            }
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

    public virtual void ToggleGroupVisibility(int pos, bool to)
    {
        groups[pos].SetVisibility(to);
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

    public virtual void SetVisibility(bool to)
    {
        btn.IsVisible = to;
        element.IsVisible = false; // might as well alway hide the content, it should be shown by the code handling hiding anyways
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

public class UITabGroup_GroupToggleButton : UITabGroup_Group
{
    protected new Common_ButtonToggle btn;

    public UITabGroup_GroupToggleButton(Control element, Common_ButtonToggle btn) : base(element, btn)
    {
        this.btn = btn;
        btn.autoToggle = false;
    }

    public override void Setup(UITabGroup master, int index)
    {
        btn.Register(async _ => await master.ChangeSelection(index), null);
        Close();
    }

    public override Task Close()
    {
        btn.isSelected = false;
        element.IsVisible = false;
        return Task.CompletedTask;
    }

    public override Task Open()
    {
        btn.isSelected = true;
        element.IsVisible = true;
        return Task.CompletedTask;
    }
}
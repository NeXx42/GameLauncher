using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_TagUnselectable : UserControl
{
    public Library_TagUnselectable()
    {
        InitializeComponent();
    }

    public void Draw(TagDto? tag)
    {
        if (tag == null)
        {
            IsVisible = false;
            return;
        }

        IsVisible = true;
        txt.Text = tag.name;
    }
}
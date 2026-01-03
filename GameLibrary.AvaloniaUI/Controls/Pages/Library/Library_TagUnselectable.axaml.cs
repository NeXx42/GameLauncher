using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_TagUnselectable : UserControl
{
    public Library_TagUnselectable()
    {
        InitializeComponent();
    }

    public void Draw(int? tagNumber)
    {
        if (!tagNumber.HasValue)
        {
            IsVisible = false;
            return;
        }

        IsVisible = true;
        txt.Text = LibraryHandler.GetTagName(tagNumber.Value);
    }
}
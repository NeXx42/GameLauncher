using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB.Tables;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_Tag : UserControl
{
    private Action? clickEvent;

    public Library_Tag()
    {
        InitializeComponent();
        border.PointerPressed += (_, __) => clickEvent?.Invoke();
        //border.Margin = new Thickness(0, 0, 5, 5);
    }

    public void Draw(dbo_Tag tag, Action<int> onClick)
    {
        Toggle(false);

        clickEvent = () => onClick?.Invoke(tag.TagId);
        txt.Text = tag.TagName.Replace("\n", "");
    }

    public void Toggle(bool to)
    {
        border.Background = to ? new SolidColorBrush(Color.FromRgb(183, 156, 0)) : new SolidColorBrush(Color.FromRgb(51, 51, 51));
    }
}
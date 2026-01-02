using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public partial class Control_Settings_Title : UserControl, ISettingControl
{
    public Control_Settings_Title()
    {
        InitializeComponent();
    }

    public Task LoadValue() => Task.CompletedTask;

    public ISettingControl Draw(SettingsUI_Title title)
    {
        lbl.Margin = new Thickness(0, title.margin, 0, 0);
        lbl.Content = title.title;
        return this;
    }
}
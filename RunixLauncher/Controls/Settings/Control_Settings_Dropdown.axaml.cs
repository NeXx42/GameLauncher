using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public partial class Control_Settings_Dropdown : UserControl, ISettingControl
{
    private SettingBase? settings;

    public Control_Settings_Dropdown()
    {
        InitializeComponent();
    }

    public ISettingControl Draw(SettingBase settings, SettingsUI_Dropdown ui)
    {
        this.settings = settings;

        title.Content = settings.getName;
        btn.Setup(ui.options, 0, SaveValue);

        return this;
    }

    public async Task LoadValue()
    {
        int selected = await settings!.LoadSetting<int>();
        btn.SilentlyChangeValue(selected);
    }

    private async Task SaveValue()
    {
        await settings!.SaveSetting(btn.selectedIndex);
    }
}
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Settings;

public partial class Control_Settings_Toggle : UserControl, ISettingControl
{
    private bool selectedOption = false;
    private SettingBase setting;

    public Control_Settings_Toggle()
    {
        InitializeComponent();
        btn.RegisterClick(Toggle);
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Toggle info)
    {
        this.setting = setting;
        title.Content = setting.getName;

        return this;
    }

    public async Task LoadValue()
    {
        object o = await setting.LoadSetting();
        selectedOption = ((string)o ?? "") == "1";

        RedrawButton();
    }

    private async Task Toggle()
    {
        if (await setting.SaveSetting(!selectedOption))
        {
            selectedOption = !selectedOption;
            RedrawButton();
        }
    }

    private void RedrawButton()
    {
        btn.Label = selectedOption ? "Enabled" : "Disabled";
    }
}
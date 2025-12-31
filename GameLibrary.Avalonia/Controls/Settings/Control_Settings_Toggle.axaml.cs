using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Controls.Settings;

public partial class Control_Settings_Toggle : UserControl, ISettingControl
{
    private bool selectedOption = false;
    private SettingBase? setting;

    public Control_Settings_Toggle()
    {
        InitializeComponent();
        btn.RegisterClick(Toggle, "Updating");
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Toggle info)
    {
        this.setting = setting;
        title.Content = setting.getName;

        return this;
    }

    public async Task LoadValue()
    {
        selectedOption = await setting!.LoadSetting<bool>();
        RedrawButton();
    }

    private async Task Toggle()
    {
        if (await setting!.SaveSetting(!selectedOption))
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
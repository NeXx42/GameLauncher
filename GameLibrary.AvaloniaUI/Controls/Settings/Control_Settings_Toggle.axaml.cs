using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public partial class Control_Settings_Toggle : UserControl, ISettingControl
{
    private bool selectedOption = false;
    private SettingBase? setting;

    private string? posText;
    private string? negText;

    public Control_Settings_Toggle()
    {
        InitializeComponent();
        btn.RegisterClick(Toggle, "Saving");
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Toggle info)
    {
        this.setting = setting;
        title.Content = setting.getName;

        posText = info.positiveText;
        negText = info.negativeText;

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
        btn.Label = selectedOption ? (posText ?? "Enabled") : (negText ?? "Disabled");
    }
}
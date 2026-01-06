using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public partial class Control_Settings_DefaultFilter : UserControl, ISettingControl
{
    private SettingBase setting;
    private bool isAscending;

    public Control_Settings_DefaultFilter(SettingBase setting)
    {
        InitializeComponent();

        this.setting = setting;

        type.SetupAsync(GameFilterRequest.getOrderOptionsNames, 0, Save);
        dir.RegisterClick(FlipDirection);
    }

    public async Task LoadValue()
    {
        int? res = await setting.LoadSetting<int>();
        (int order, isAscending) = GameFilterRequest.DecodeOrder(res ?? 0);

        type.SilentlyChangeValue(order);
        RedrawDirection();
    }

    private async Task FlipDirection()
    {
        isAscending = !isAscending;
        await Save();

        RedrawDirection();
    }

    private async Task Save() => await setting.SaveSetting(GameFilterRequest.EncodeOrder(type.selectedIndex, isAscending));
    private void RedrawDirection() => dir.Label = isAscending ? "Ascending" : "Descending";
}
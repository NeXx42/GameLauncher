using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GameLibrary.AvaloniaUI.Controls.Settings;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.SubPage;

public partial class Popup_Settings : UserControl
{
    private UITabGroup tabGroup;
    private List<ISettingControl> activeControls = new List<ISettingControl>();

    public Popup_Settings()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        DrawSettings();
    }

    private void DrawSettings()
    {
        List<UITabGroup_Group> groups = new List<UITabGroup_Group>();

        Application.Current!.TryGetResource("btn_Background", out object? bg);
        Application.Current!.TryGetResource("btn_Border", out object? border);
        SolidColorBrush btnColour = (bg as SolidColorBrush)!;
        SolidColorBrush btnBorderColour = (border as SolidColorBrush)!;

        foreach (KeyValuePair<string, SettingBase[]> settings in ConfigHandler.groupedSettings!)
        {
            Border btn = new Border();
            btn.CornerRadius = new CornerRadius(2);
            btn.Background = btnColour;
            btn.BorderBrush = btnBorderColour;
            btn.BorderThickness = new Thickness(1);
            btn.Height = 30;

            Label l = new Label();
            l.Content = settings.Key;
            l.Padding = new Thickness(5);
            l.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            l.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            btn.Child = l;

            StackPanel grid = CreateGroup(settings.Value);

            tabs.Children.Add(btn);
            content.Children.Add(grid);

            groups.Add(new UITabGroup_Group(grid, btn));
        }

        tabGroup = new UITabGroup(groups.ToArray());
        tabGroup.ChangeSelection(0);

        StackPanel CreateGroup(SettingBase[] settings)
        {
            StackPanel stack = new StackPanel();
            stack.Margin = new Thickness(10);

            foreach (SettingBase setting in settings)
            {
                if (!ConfigHandler.IsSettingSupported(setting.getCompatibility))
                    continue;

                ISettingControl? control = CreateSetting(setting);

                if (control != null && control is UserControl uc)
                {
                    activeControls.Add(control);

                    uc.Margin = new Thickness(0, 5, 0, 0);
                    stack.Children.Add(uc);
                }
            }

            return stack;
        }

        ISettingControl? CreateSetting(SettingBase setting)
        {
            switch (setting.GetUI())
            {
                case SettingsUI_DirectorySelector settingsUI_DirectorySelector: return new Control_Settings_DirectorySelector().Draw(setting, settingsUI_DirectorySelector);
                case SettingsUI_Toggle settingsUI_Toggle: return new Control_Settings_Toggle().Draw(setting, settingsUI_Toggle);

                case SettingsUI_Runners settingsUI_Runners: return new Control_Settings_Runners().Draw(setting, settingsUI_Runners);

                case SettingsUI_Title settingsUI_Title: return new Control_Settings_Title().Draw(settingsUI_Title);
            }

            return null;
        }
    }

    public async Task OnOpen()
    {
        foreach (ISettingControl setting in activeControls)
        {
            await setting.LoadValue();
        }
    }
}
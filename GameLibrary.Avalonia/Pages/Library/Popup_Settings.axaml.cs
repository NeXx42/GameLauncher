using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Avalonia.Controls.Settings;
using GameLibrary.Avalonia.Settings;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_Settings : UserControl
{
    private List<ISettingControl> activeControls = new List<ISettingControl>();

    public Popup_Settings()
    {
        InitializeComponent();
        DrawSettings();
    }

    private void DrawSettings()
    {
        foreach (KeyValuePair<string, SettingBase[]> settings in ConfigHandler.groupedSettings)
        {
            Label groupName = new Label();
            groupName.Content = settings.Key;
            groupName.FontSize = 16;
            groupName.Margin = new Thickness(0, 15, 0, 0);

            content.Children.Add(groupName);
            content.Children.Add(CreateGroup(settings.Value));
        }

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
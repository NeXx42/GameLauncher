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
    private UITabGroup? tabGroup;
    private List<ISettingControl> activeControls = new List<ISettingControl>();

    public Popup_Settings()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        DrawSettings();
    }

    private async void DrawSettings()
    {
        List<UITabGroup_Group> groups = new List<UITabGroup_Group>();

        foreach (KeyValuePair<string, SettingBase[]> settings in ConfigHandler.groupedSettings!)
        {
            Common_ButtonToggle btn = new Common_ButtonToggle();
            btn.Label = settings.Key;
            btn.Height = 30;

            StackPanel grid = CreateGroup(settings.Value);

            tabs.Children.Add(btn);
            content.Children.Add(grid);

            groups.Add(new Popup_TabGroup(grid, btn));
        }

        tabGroup = new UITabGroup(groups.ToArray());
        await tabGroup.ChangeSelection(0);

        StackPanel CreateGroup(SettingBase[] settings)
        {
            StackPanel stack = new StackPanel();
            stack.Margin = new Thickness(10);
            stack.Spacing = 5;

            foreach (SettingBase setting in settings)
            {
                if (!ConfigHandler.IsSettingSupported(setting.getCompatibility))
                    continue;

                ISettingControl? control = CreateSetting(setting);

                if (control != null && control is UserControl uc)
                {
                    activeControls.Add(control);

                    uc.Margin = new Thickness(control is Control_Settings_Title ? 0 : 15, uc.Margin.Top, uc.Margin.Right, uc.Margin.Bottom);
                    stack.Children.Add(uc);
                }
            }

            return stack;
        }

        ISettingControl? CreateSetting(SettingBase setting)
        {
            switch (setting.GetUI())
            {
                case SettingsUI_Title settingsUI_Title: return new Control_Settings_Title().Draw(settingsUI_Title);
                case SettingsUI_DirectorySelector settingsUI_DirectorySelector: return new Control_Settings_DirectorySelector().Draw(setting, settingsUI_DirectorySelector);
                case SettingsUI_Toggle settingsUI_Toggle: return new Control_Settings_Toggle().Draw(setting, settingsUI_Toggle);
                case SettingsUI_Dropdown settingsUI_Dropdown: return new Control_Settings_Dropdown().Draw(setting, settingsUI_Dropdown);

                case SettingsUI_Runners settingsUI_Runners: return new Control_Settings_Runners().Draw(setting, settingsUI_Runners);
                case SettingsUI_DefaultFilter settingsUI_DefaultFilter: return new Control_Settings_DefaultFilter(setting);
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


    internal class Popup_TabGroup : UITabGroup_Group
    {
        protected Common_ButtonToggle toggle;

        public Popup_TabGroup(Control element, Common_ButtonToggle btn) : base(element, btn)
        {
            this.toggle = btn;
            btn.autoToggle = false;
        }

        public override void Setup(UITabGroup master, int index)
        {
            toggle.Register(async (_) => await master.ChangeSelection(index));
            element.IsVisible = false;
        }

        public override Task Open()
        {
            toggle.isSelected = true;
            return base.Open();
        }

        public override Task Close()
        {
            toggle.isSelected = false;
            return base.Close();
        }
    }
}
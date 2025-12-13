using GameLibary.Components.Settings;
using GameLibary.Source;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_Indexer.xaml
    /// </summary>
    public partial class Control_Settings : UserControl
    {
        private Dictionary<ConfigHandler.ConfigValues, ISettingControl> settingControls;

        public Control_Settings()
        {
            InitializeComponent();

            btn_Save.MouseLeftButtonDown += async (_, __) => await SaveSettings();
        }

        public async Task OnOpen()
        {
            if (settingControls != null)
                return;

            settingControls = new Dictionary<ConfigHandler.ConfigValues, ISettingControl>();
            Dictionary<string, ConfigHandler.ConfigSetting[]> settings = ConfigHandler.GetConfigSettings().GroupBy(x => x.header).ToDictionary(x => x.Key, x => x.ToArray());


            foreach(var group in settings)
            {
                Label l = new Label();
                l.Content = group.Key;
                l.Foreground = new SolidColorBrush(Color.FromArgb(254,254,254,254));
                l.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                container!.Children.Add(l);

                foreach (var setting in group.Value)
                {
                    UserControl? control = RenderSetting(setting);

                    if (control == null || control is not ISettingControl settingControl)
                        continue;

                    settingControl.Draw(setting);

                    settingControls.Add(setting.configValue, settingControl);
                    container!.Children.Add(control);
                }
            }

            await LoadSettings();
        }

        private async Task LoadSettings()
        {
            foreach(var setting in settingControls)
            {
                setting.Value.LoadValue((await ConfigHandler.GetConfigValue(setting.Key))?.value);
            }
        }


        private UserControl? RenderSetting(ConfigHandler.ConfigSetting setting)
        {
            switch (setting.type)
            {
                case ConfigHandler.ConfigSettingType.File:
                case ConfigHandler.ConfigSettingType.Folder:
                    return new SettingControl_Location();

                case ConfigHandler.ConfigSettingType.String:
                    return new SettingControl_String();
            }

            return null;
        }

        private async Task SaveSettings()
        {
            int count = 0;

            foreach (var setting in settingControls)
            {
                if (!setting.Value.GetSaveValue(out string? change))
                    continue;

                count++;
                await ConfigHandler.SaveConfigValue(setting.Key, change ?? "");
            }

            MessageBox.Show($"Saved {count} settings.", "Saved", MessageBoxButton.OK);
        }
    }
}

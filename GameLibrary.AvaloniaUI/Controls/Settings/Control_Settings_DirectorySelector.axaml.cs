using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public partial class Control_Settings_DirectorySelector : UserControl, ISettingControl
{
    private SettingBase? setting;

    public Control_Settings_DirectorySelector()
    {
        InitializeComponent();

        btn.RegisterClick(SelectDirectory);
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_DirectorySelector info)
    {
        this.setting = setting;
        title.Content = setting.getName;

        return this;
    }

    public async Task LoadValue()
    {
        if (setting == null)
            return;

        UpdateLabel(await setting.LoadSetting<string>());
    }

    private async Task SelectDirectory()
    {
        SettingsUI_DirectorySelector constraints = (SettingsUI_DirectorySelector)setting!.GetUI();

        if (constraints.folder)
        {
            string? selectedFolder = await DependencyManager.OpenFolderDialog(setting.getName);

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                await setting.SaveSetting(selectedFolder);
                UpdateLabel(selectedFolder);
            }
        }
        else
        {

        }
    }

    private void UpdateLabel(string? to)
    {
        btn.Label = string.IsNullOrEmpty(to) ? "Select Folder" : to;
    }
}
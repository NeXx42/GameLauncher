using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Settings;

public partial class Control_Settings_DirectorySelector : UserControl, ISettingControl
{
    private SettingBase setting;

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
        SettingsUI_DirectorySelector constraints = (SettingsUI_DirectorySelector)setting.GetUI();

        if (constraints.folder)
        {
            var selectedFolders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
            {
                Title = setting.getName,
                AllowMultiple = false
            });

            if (selectedFolders.Count == 1)
            {
                await setting.SaveSetting(selectedFolders[0].Path.AbsolutePath);
                UpdateLabel(selectedFolders[0].Path.AbsolutePath);
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
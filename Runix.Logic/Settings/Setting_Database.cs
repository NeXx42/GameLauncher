using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Database : SettingBase
{
    public override string getName => "Database";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Toggle("Change", "WHAT???");

    public override Task<T?> LoadSetting<T>() where T : default
    {
        return Task.FromResult((T?)(object)true);
    }

    public override async Task<bool> SaveSetting<T>(T val)
    {
        int res = await DependencyManager.OpenConfirmationAsync("Change Database", "Do you want to either move, or select another database?",
            ("Select Other", SelectDatabase, "Loading"),
            ("Move Existing", MoveDatabase, "Loading")
        );

        return false;
    }

    private async Task SelectDatabase()
    {
        string? path = await DependencyManager.OpenFileDialog("Select database", "*.db");

        if (string.IsNullOrEmpty(path))
            return;

        await DependencyManager.CreateDBPointerFile(path!);
        await DependencyManager.OpenConfirmationAsync("Selected database", "The alternative database has been selected. Restart the application to continue");
        DependencyManager.Quit();
    }

    private async Task MoveDatabase()
    {
        string? path = await DependencyManager.OpenFolderDialog("Select database folder");

        if (string.IsNullOrEmpty(path))
            return;

        //await DependencyManager.CreateDBPointerFile(Path.Combine(path, $"{DependencyManager.APPLICATION_NAME}.db"));
    }
}

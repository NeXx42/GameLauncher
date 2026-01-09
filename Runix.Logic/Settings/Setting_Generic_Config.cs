using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Generic_Config : SettingBase
{
    private string settingName;

    private SettingOSCompatibility compatibility;

    private ConfigKeys configValue;
    private ISettingsUI uiSettings;

    private bool requiresRestart;

    public Setting_Generic_Config(string settingName, SettingOSCompatibility compatibility, ConfigKeys configValue, ISettingsUI uiSettings, bool requiresRestart = false)
    {
        this.settingName = settingName;
        this.compatibility = compatibility;
        this.configValue = configValue;
        this.uiSettings = uiSettings;

        this.requiresRestart = requiresRestart;
    }

    public override string getName => settingName;

    public override SettingOSCompatibility getCompatibility => compatibility;
    public override ISettingsUI GetUI() => uiSettings;


    public override async Task<T?> LoadSetting<T>() where T : default
        => ConfigHandler.configProvider!.GetGeneric<T>(configValue);

    public override async Task<bool> SaveSetting<T>(T val)
    {
        if (!await ConfigHandler.configProvider!.SaveGeneric(configValue, val))
            return false;

        if (requiresRestart)
            await DependencyManager.OpenConfirmationAsync("Requires restart", $"Changing the settings {settingName} requires a restart to take effect");

        return true;
    }
}

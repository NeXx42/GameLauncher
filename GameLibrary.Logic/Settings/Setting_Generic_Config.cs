using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Generic_Config : SettingBase
{
    private string settingName;
    private SettingOSCompatibility compatibility;
    private ConfigHandler.ConfigValues configValue;
    private ISettingsUI uiSettings;
    private ConfigHandler.ConfigSerialization serializationType;

    public Setting_Generic_Config(string settingName, SettingOSCompatibility compatibility, ConfigHandler.ConfigValues configValue, ISettingsUI uiSettings, ConfigHandler.ConfigSerialization serializationType)
    {
        this.settingName = settingName;
        this.compatibility = compatibility;
        this.configValue = configValue;
        this.uiSettings = uiSettings;
        this.serializationType = serializationType;
    }

    public override string getName => settingName;

    public override SettingOSCompatibility getCompatibility => compatibility;

    public override ISettingsUI GetUI() => uiSettings;

    public override async Task<T?> LoadSetting<T>() where T : default => await LoadSettingAsConfig<T>(configValue);
    public override async Task<bool> SaveSetting(object val) => await SaveSettingAsConfig(configValue, val, serializationType);
}

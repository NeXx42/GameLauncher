using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public abstract class SettingBase
{
    public abstract string getName { get; }
    public abstract SettingOSCompatibility getCompatibility { get; }


    public abstract Task<T?> LoadSetting<T>();
    public abstract Task<bool> SaveSetting(object val);

    public abstract ISettingsUI GetUI();

    public virtual async Task<T?> LoadSettingAsConfig<T>(ConfigHandler.ConfigValues configName)
        => await ConfigHandler.GetConfigValue<T?>(configName, default);

    public virtual async Task<bool> SaveSettingAsConfig(ConfigHandler.ConfigValues configName, object obj, ConfigHandler.ConfigSerialization configSerialization)
        => await ConfigHandler.SaveConfigValue(configName, obj, configSerialization);
}

public enum SettingOSCompatibility
{
    Universal,
    Linux,
    Windows
}
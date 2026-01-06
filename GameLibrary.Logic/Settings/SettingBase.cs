using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public abstract class SettingBase
{
    public abstract string getName { get; }
    public abstract SettingOSCompatibility getCompatibility { get; }


    public abstract Task<T?> LoadSetting<T>();
    public abstract Task<bool> SaveSetting(object val);

    public abstract ISettingsUI GetUI();

    public virtual async Task<T?> LoadSettingAsConfig<T>(ConfigKeys configName)
        => await ConfigHandler.configProvider!.GetGeneric<T>(configName);

    public virtual async Task<bool> SaveSettingAsConfig<T>(ConfigKeys configName, T obj)
        => await ConfigHandler.configProvider!.SaveGeneric(configName, obj);
}

public enum SettingOSCompatibility
{
    Universal,
    Linux,
    Windows
}
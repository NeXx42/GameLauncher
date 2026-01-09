using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public abstract class SettingBase
{
    public abstract string getName { get; }
    public abstract SettingOSCompatibility getCompatibility { get; }


    public abstract Task<T?> LoadSetting<T>();
    public abstract Task<bool> SaveSetting<T>(T val);

    public abstract ISettingsUI GetUI();
}

public enum SettingOSCompatibility
{
    Universal,
    Linux,
    Windows
}
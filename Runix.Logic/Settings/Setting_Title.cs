using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Title : SettingBase
{
    public readonly string name;
    public readonly double margin;
    public readonly SettingOSCompatibility compatibility;

    public Setting_Title(string name, double margin, SettingOSCompatibility compatibility)
    {
        this.name = name;
        this.margin = margin;
        this.compatibility = compatibility;
    }

    public override string getName => name;
    public override SettingOSCompatibility getCompatibility => compatibility;

    public override ISettingsUI GetUI() => new SettingsUI_Title(name, margin);

    public override Task<T?> LoadSetting<T>() where T : default => Task.FromResult<T?>(default);
    public override Task<bool> SaveSetting<T>(T val) => Task.FromResult(true);
}

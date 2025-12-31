using CSharpSqliteORM;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Settings.UI;

public class Setting_Runners : SettingBase
{
    public override string getName => "Runners";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Runners();

    public override async Task<T?> LoadSetting<T>() where T : default
        => (T?)(object)await Database_Manager.GetItems<dbo_Runner>();

    public override async Task<bool> SaveSetting(object val)
    {
        dbo_Runner[] profiles = (dbo_Runner[])val;
        await Database_Manager.AddOrUpdate(profiles, x => SQLFilter.Equal(nameof(x.runnerId), x.runnerId));

        return true;
    }
}

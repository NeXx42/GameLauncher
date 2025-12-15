using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Wine_Profiles : SettingBase
{
    public override string getName => "Wine Profiles";

    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Linux;
    public override ISettingsUI GetUI() => new SettingsUI_Wine_Profiles();

    public override async Task<T?> LoadSetting<T>() where T : default
     => (T?)(object)await DatabaseHandler.GetItems<dbo_WineProfile>();

    public override async Task<bool> SaveSetting(object val)
    {
        dbo_WineProfile[] profiles = (dbo_WineProfile[])val;
        await DatabaseHandler.AddOrUpdate(profiles, x => QueryBuilder.SQLEquals(nameof(x.id), x.id));

        return true;
    }
}

using CSharpSqliteORM;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Settings.UI;

public class Setting_Runners : SettingBase
{
    public override string getName => "Runners";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Runners();

    public override async Task<T?> LoadSetting<T>() where T : default
    {
        await RunnerManager.RecacheRunners();
        return (T?)(object)RunnerManager.GetRunnerProfiles();
    }

    public override Task<bool> SaveSetting<T>(T val) => Task.FromResult(false);
}

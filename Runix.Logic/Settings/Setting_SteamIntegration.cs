using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Integration;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_SteamIntegration : SettingBase
{
    public override string getName => "Steam Integration";

    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;
    public override ISettingsUI GetUI() => new SettingsUI_Toggle("Sync", "Enable");

    public override async Task<T?> LoadSetting<T>() where T : default
    {
        return (T)(object)await Database_Manager.Exists<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libraryExternalType), (int)Library_ExternalProviders.Steam));
    }

    public override async Task<bool> SaveSetting<T>(T val)
    {
        await DependencyManager.OpenConfirmationAsync("Resync?", "Update db with latest steam games",
            ("Sync", Integration_Steam.SyncLibrary, "Syncing"));

        // ui flips this if a success
        return false;
    }
}

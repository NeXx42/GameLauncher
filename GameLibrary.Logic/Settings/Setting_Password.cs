using CSharpSqliteORM;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Password : SettingBase
{
    public override string getName => "Password";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Toggle("Change Password", "Add Password");

    public override async Task<T?> LoadSetting<T>() where T : default
    {
        string? hash = ConfigHandler.configProvider!.GetValue(ConfigKeys.PasswordHash);
        return (T?)(object)!string.IsNullOrEmpty(hash);
    }

    public override async Task<bool> SaveSetting(object val)
    {
        string? result = await DependencyManager.OpenStringInputModal("Password", string.Empty, true);

        if (string.IsNullOrEmpty(result) && !await DependencyManager.OpenYesNoModal("Clear password?", "Are you sure you want to clear the password?"))
            return false;

        result = string.IsNullOrEmpty(result) ? null : EncryptionHelper.EncryptPassword(result);
        return await ConfigHandler.configProvider!.SaveValue(ConfigKeys.PasswordHash, result);
    }
}

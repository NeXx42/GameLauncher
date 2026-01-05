using CSharpSqliteORM;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_Password : SettingBase
{
    public override string getName => "Password";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Toggle("Change Password", "Add Password");

    public override async Task<T?> LoadSetting<T>() where T : default
    {
        string? hash = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.PasswordHash, string.Empty);
        return (T?)(object)!string.IsNullOrEmpty(hash);
    }

    public override async Task<bool> SaveSetting(object val)
    {
        string? result = await DependencyManager.OpenStringInputModal("Password", string.Empty, true);

        if (string.IsNullOrEmpty(result))
        {
            if (!await DependencyManager.OpenYesNoModal("Clear password?", "Are you sure you want to clear the password?"))
            {
                return false;
            }

            await ConfigHandler.DeleteConfigValue(ConfigHandler.ConfigValues.PasswordHash);
            return true;
        }

        await ConfigHandler.SaveConfigValue(ConfigHandler.ConfigValues.PasswordHash, EncryptionHelper.EncryptPassword(result));
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CSharpSqliteORM;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using Logic.db;

namespace GameLibrary.Logic
{
    public static class ConfigHandler
    {
        public static bool isOnLinux { private set; get; }

        public static ConfigProvider<ConfigKeys>? configProvider;
        public static ReadOnlyDictionary<string, SettingBase[]>? groupedSettings { get; private set; }


        public static async Task Init()
        {
            dbo_Config[] config = await Database_Manager.GetItems<dbo_Config>();
            configProvider = new ConfigProvider<ConfigKeys>(config.Select(x => (x.key, x.value)), SaveConfig, DeleteConfig);

            isOnLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            RegisterSettings();
        }

        private static void RegisterSettings()
        {
            Dictionary<string, SettingBase[]> settings = new Dictionary<string, SettingBase[]>
            {
                {
                    "Application",
                    [
                        new Setting_Title("Security", 0, SettingOSCompatibility.Universal),
                        new Setting_Password(),
                        new Setting_Title("Database", 10, SettingOSCompatibility.Universal),
                        new Setting_Database(),
                        new Setting_Title("Appearance", 10, SettingOSCompatibility.Universal),
                        new Setting_Generic_Config("Default Filter", SettingOSCompatibility.Universal, ConfigKeys.Appearance_DefaultFilter, new SettingsUI_DefaultFilter()),
                        new Setting_Generic_Config("Page Layout", SettingOSCompatibility.Universal, ConfigKeys.Appearance_Layout, new SettingsUI_Dropdown(["Paginated", "Endless"])),
                        new Setting_Generic_Config("Disable background images", SettingOSCompatibility.Universal, ConfigKeys.Appearance_BackgroundImage, new SettingsUI_Toggle()),
                        new Setting_Title("Importing", 10, SettingOSCompatibility.Universal),
                        new Setting_Generic_Config("Unique folder import", SettingOSCompatibility.Universal, ConfigKeys.Import_GUIDFolderNames, new SettingsUI_Toggle()),
                    ]
                },
                {
                    "Libraries",
                    [
                        new Setting_Title("Libraries", 0, SettingOSCompatibility.Universal),
                        new Setting_Title("Integration", 0, SettingOSCompatibility.Universal),
                        new Setting_SteamIntegration(),
                    ]
                },
                {
                    "Runner",
                    [
                        new Setting_Title("Runners", 0, SettingOSCompatibility.Linux),
                        new Setting_Runners(),

                        new Setting_Title("Misc", 10, SettingOSCompatibility.Universal),
                        new Setting_Generic_Config("Concurrency", SettingOSCompatibility.Universal, ConfigKeys.Launcher_Concurrency, new SettingsUI_Toggle()),
                    ]
                },
                {
                    "Sandboxing",
                    [
                        new Setting_Title("Firejail", 0, SettingOSCompatibility.Linux),
                        new Setting_Generic_Config("Use firejail", SettingOSCompatibility.Linux, ConfigKeys.Sandbox_Linux_Firejail_Enabled, new SettingsUI_Toggle()),
                        new Setting_Generic_Config("Block networktivity", SettingOSCompatibility.Linux, ConfigKeys.Sandbox_Linux_Firejail_Networking, new SettingsUI_Toggle()),
                        new Setting_Generic_Config("Isolate filesystem", SettingOSCompatibility.Linux, ConfigKeys.Sandbox_Linux_Firejail_FileSystemIsolation, new SettingsUI_Toggle()),

                        new Setting_Title("Sandboxie", 10, SettingOSCompatibility.Windows),
                        new Setting_Generic_Config("Sandboxie box name", SettingOSCompatibility.Windows, ConfigKeys.Sandbox_Windows_SandieboxBox, new SettingsUI_Toggle()),
                        new Setting_Generic_Config("Sandboxie location", SettingOSCompatibility.Windows, ConfigKeys.Sandbox_Windows_SandieboxLocation, new SettingsUI_DirectorySelector(){ folder = true}),
                    ]
                }
            };


            groupedSettings = new ReadOnlyDictionary<string, SettingBase[]>(settings);
        }

        private static async Task SaveConfig(string key, string value)
        {
            dbo_Config dbo = new dbo_Config()
            {
                key = key,
                value = value,
            };

            await Database_Manager.AddOrUpdate(dbo, SQLFilter.Equal(nameof(dbo_Config.key), key), nameof(dbo_Config.value));
        }

        private static Task DeleteConfig(string key)
            => Database_Manager.Delete<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), key));


        public static bool IsSettingSupported(SettingOSCompatibility compatibility)
        {
            if (compatibility == SettingOSCompatibility.Universal)
                return true;

            return (compatibility == SettingOSCompatibility.Linux && isOnLinux) || (compatibility == SettingOSCompatibility.Windows && !isOnLinux);
        }
    }
}

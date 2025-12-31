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
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using Logic.db;

namespace GameLibrary.Logic
{
    public static class ConfigHandler
    {
        public static bool isOnLinux { private set; get; }

        public enum ConfigSerialization
        {
            String,
            Boolean,
            FolderDirectory
        }

        public enum ConfigValues
        {
            RootPath,
            EmulatorPath,
            PasswordHash,

            Launcher_Concurrency,

            Sandbox_Windows_SandieboxBox,
            Sandbox_Windows_SandieboxLocation,
            Sandbox_Linux_Firejail_Enabled,
            Sandbox_Linux_Firejail_FileSystemIsolation,
            Sandbox_Linux_Firejail_Networking,

            Import_GUIDFolderNames,

            Proton_SteamFolder,
        }

        public static ReadOnlyDictionary<string, SettingBase[]>? groupedSettings { get; private set; }


        public static async Task Init()
        {
            isOnLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            RegisterSettings();
        }

        private static void RegisterSettings()
        {
            Dictionary<string, SettingBase[]> settings = new Dictionary<string, SettingBase[]>();

            settings.Add("Importing", [
                new Setting_Generic_Config("Unique folder import", SettingOSCompatibility.Universal, ConfigValues.Import_GUIDFolderNames, new SettingsUI_Toggle(), ConfigSerialization.Boolean),
            ]);

            settings.Add("Runner", [
                new Setting_Runners(),
                new Setting_Generic_Config("Concurrency", SettingOSCompatibility.Universal, ConfigValues.Launcher_Concurrency, new SettingsUI_Toggle(), ConfigSerialization.Boolean),
            ]);

            settings.Add("Sandboxing", [
                new Setting_Generic_Config("Use firejail", SettingOSCompatibility.Linux, ConfigValues.Sandbox_Linux_Firejail_Enabled, new SettingsUI_Toggle(), ConfigSerialization.Boolean),
                new Setting_Generic_Config("Block networktivity", SettingOSCompatibility.Linux, ConfigValues.Sandbox_Linux_Firejail_Networking, new SettingsUI_Toggle(), ConfigSerialization.Boolean),
                new Setting_Generic_Config("Isolate filesystem", SettingOSCompatibility.Linux, ConfigValues.Sandbox_Linux_Firejail_FileSystemIsolation, new SettingsUI_Toggle(), ConfigSerialization.Boolean),

                new Setting_Generic_Config("Sandboxie box name", SettingOSCompatibility.Windows, ConfigValues.Sandbox_Windows_SandieboxBox, new SettingsUI_Toggle(), ConfigSerialization.Boolean),
                new Setting_Generic_Config("Sandboxie location", SettingOSCompatibility.Windows, ConfigValues.Sandbox_Windows_SandieboxLocation, new SettingsUI_DirectorySelector(){ folder = true}, ConfigSerialization.FolderDirectory),
            ]);


            groupedSettings = new ReadOnlyDictionary<string, SettingBase[]>(settings);
        }





        public static string? SerializeConfigObject<T>(T val)
        {
            if (val == null)
            {
                throw new Exception("Tried to serialize null");
            }

            switch (val)
            {
                case bool b: return b ? "1" : "0";
                case string s: return s;
            }

            return null;
        }

        public static T DeserializeConfigValue<T>(object inp)
            => DeserializeConfigValue<T>((string)inp);

        public static T DeserializeConfigValue<T>(string inp)
        {
            switch (typeof(T).Name)
            {
                case nameof(Boolean): return (T)(object)(inp == "1" ? true : false);

                case nameof(Object):
                case nameof(String): return (T)(object)inp;
            }

            throw new Exception("Unhandled type");
        }

        public static (bool, string?) ValidateObjectSerialization(object val, ConfigSerialization serializeTo)
        {
            try
            {
                switch (serializeTo)
                {
                    case ConfigSerialization.Boolean: return (true, SerializeConfigObject((bool)val));
                    case ConfigSerialization.String: return (true, SerializeConfigObject((string)val));

                    case ConfigSerialization.FolderDirectory:
                        string f_dir = (string)val;

                        if (!Directory.Exists(f_dir))
                            return (false, null);

                        return (true, SerializeConfigObject(f_dir));
                }
            }
            catch
            {
                return (false, null);
            }

            return (false, null);
        }




        public static async Task<T> GetConfigValue<T>(ConfigValues config, T defaultVal)
        {
            dbo_Config? configValue = await Database_Manager.GetItem<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), config.ToString()));

            if (configValue == null)
                return defaultVal;

            return DeserializeConfigValue<T>(configValue.value);
        }



        public static async Task<bool> SaveConfigValue(ConfigValues config, object obj, ConfigSerialization configSerialization)
        {
            (bool valid, string? res) = ValidateObjectSerialization(obj, configSerialization);

            if (valid && !string.IsNullOrEmpty(res))
            {
                await SaveConfigValue_Internal(config, res!);
                return true;
            }

            return false;
        }

        public static async Task<bool> SaveConfigValue<T>(ConfigValues config, T setting)
        {
            string? result = SerializeConfigObject(setting);

            if (result == null)
                return false;

            await SaveConfigValue_Internal(config, result);
            return true;
        }

        private static async Task SaveConfigValue_Internal(ConfigValues config, string serializedVal)
        {
            dbo_Config val = new dbo_Config()
            {
                key = config.ToString(),
                value = serializedVal
            };

            await Database_Manager.AddOrUpdate(val, SQLFilter.Equal(nameof(dbo_Config.key), val.key), nameof(dbo_Config.value));
        }

        public static bool IsSettingSupported(SettingOSCompatibility compatibility)
        {
            if (compatibility == SettingOSCompatibility.Universal)
                return true;

            return (compatibility == SettingOSCompatibility.Linux && isOnLinux) || (compatibility == SettingOSCompatibility.Windows && !isOnLinux);
        }
    }
}

using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source
{
    public static class ConfigHandler
    {
        public enum ConfigValues
        {
            RootPath,
            EmulatorPath,
            PasswordHash,
            SandieboxBox,
            SandieboxLocation
        }

        public enum ConfigSettingType
        {
            String,
            Password,
            Int,
            Boolean,
            Folder,
            File
        }


        public static ConfigSetting[] configSettings = [
            new ConfigSetting("Paths", ConfigValues.RootPath, ConfigSettingType.Folder),

            new ConfigSetting("Emulation", ConfigValues.EmulatorPath, ConfigSettingType.File),
            new ConfigSetting("Emulation", ConfigValues.SandieboxBox, ConfigSettingType.String),
            new ConfigSetting("Emulation", ConfigValues.SandieboxLocation, ConfigSettingType.File),
        ];


        public static async Task<dbo_Config?> GetConfigValue(ConfigValues config)
        {
            return await DatabaseHandler.GetItem<dbo_Config>(QueryBuilder.SQLEquals(nameof(dbo_Config.key), config.ToString()));
        }

        public static async Task SaveConfigValue(ConfigValues config, string setting)
        {
            QueryBuilder.InternalAccessor selector = QueryBuilder.SQLEquals(nameof(dbo_Config.key), config.ToString());

            dbo_Config val = new dbo_Config()
            {
                key = config.ToString(),
                value = setting
            };

            if (await DatabaseHandler.Exists<dbo_Config>(selector))
            {
                await DatabaseHandler.UpdateTableEntry(val, selector);
            }
            else
            {
                await DatabaseHandler.InsertIntoTable(val);
            }
        }


        public static ConfigSetting[] GetConfigSettings() => configSettings;


        public struct ConfigSetting
        {
            public string header;
            public ConfigValues configValue;
            public ConfigSettingType type;

            public ConfigSetting(string header, ConfigValues val, ConfigSettingType t)
            {
                this.header = header;
                this.configValue = val;
                this.type = t;
            }
        }
    }
}

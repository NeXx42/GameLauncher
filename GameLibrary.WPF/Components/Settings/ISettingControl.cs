using GameLibary.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Components.Settings
{
    public interface ISettingControl
    {
        public void Draw(ConfigHandler.ConfigSetting setting);

        public void LoadValue(string? val);
        public bool GetSaveValue(out string? val);
    }
}

using GameLibary.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameLibary.Components.Settings
{
    /// <summary>
    /// Interaction logic for SettingControl_String.xaml
    /// </summary>
    public partial class SettingControl_String : UserControl, ISettingControl
    {
        private bool isChanged = false;

        public SettingControl_String()
        {
            InitializeComponent();
        }

        public void Draw(ConfigHandler.ConfigSetting setting)
        {
            label.Content = setting.configValue.ToString();

            inp.TextChanged += (_, __) =>
            {
                isChanged = true;
            };
        }

        public bool GetSaveValue(out string? val)
        {
            val = inp.Text;
            return isChanged;
        }

        public void LoadValue(string? val)
        {
            inp.Text = val;
        }
    }
}

using GameLibary.Source;
using Microsoft.WindowsAPICodePack.Dialogs;
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
    /// Interaction logic for SettingControl_Folder.xaml
    /// </summary>
    public partial class SettingControl_Location : UserControl, ISettingControl
    {
        private bool isChanged;
        private bool isFolderSelector;
        private string? currentValue;

        public SettingControl_Location()
        {
            InitializeComponent();
        }

        public void Draw(ConfigHandler.ConfigSetting setting)
        {
            label.Content = setting.configValue.ToString();
            isFolderSelector = setting.type == ConfigHandler.ConfigSettingType.Folder;

            selector.MouseLeftButtonDown += (_, __) => OpenPicker();
        }


        private void OpenPicker()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = isFolderSelector,
                Title = isFolderSelector ? "Select Folder" : "Select File"
            };

            if(dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                isChanged = true;

                currentValue = dlg.FileName;
                RenderValue();
            }
        }


        public bool GetSaveValue(out string? val) 
        { 
            val = currentValue;
            return isChanged;
        }

        public void LoadValue(string? val)
        {
            currentValue = val;
            RenderValue();
        }

        private void RenderValue()
        {
            selector.Label = currentValue ?? "Select";
        }
    }
}

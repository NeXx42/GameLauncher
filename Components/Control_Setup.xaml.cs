using GameLibary.Source.Database.Tables;
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
using System.IO;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_Setup.xaml
    /// </summary>
    public partial class Control_Setup : UserControl
    {
        private string dataRoot;
        private string emulator;

        public Control_Setup()
        {
            InitializeComponent();

            inp_DataRoot.Content = MainWindow.GameRootLocation ?? "Set Location";
            inp_Emulator.Content = MainWindow.EmulatorLocation ?? "Set Location";
        }

        private void btn_Complete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanContinue())
                return;

            DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_ROOTLOCATION, value = dataRoot });
            DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_EMULATORLOCATION, value = emulator });
            DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_PASSWORD, value = inp_Password.Text });

            MainWindow.window!.CheckForSetup();
        }

        private bool CanContinue()
        {
            string password = inp_Password.Text;

            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(dataRoot) && !string.IsNullOrEmpty(emulator))
            {
                return true;
            }

            return false;
        }

        private void inp_Emulator_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Title = "Select Emulator"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok && File.Exists(dlg.FileName))
            {
                emulator = dlg.FileName;
            }

            inp_Emulator.Content = emulator ?? "Set Location";
        }

        private void inp_DataRoot_Click(object sender, RoutedEventArgs e) 
        { 
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Data Root"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok && Directory.Exists(dlg.FileName))
            {
                dataRoot = dlg.FileName;
            }

            inp_DataRoot.Content = dataRoot ?? "Set Location";
        }
    }
}

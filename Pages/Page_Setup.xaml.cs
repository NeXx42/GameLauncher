using GameLibary.Source.Database.Tables;
using GameLibary.Source;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace GameLibary.Pages
{
    /// <summary>
    /// Interaction logic for Page_Setup.xaml
    /// </summary>
    public partial class Page_Setup : Page
    {
        private string dataRoot;
        private string emulator;

        public Page_Setup()
        {
            InitializeComponent();

            inp_DataRoot.Content = MainWindow.GameRootLocation ?? "Set Location";
            inp_Emulator.Content = MainWindow.EmulatorLocation ?? "Set Location";
        }

        private async void btn_Complete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanContinue())
                return;

            await DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_ROOTLOCATION, value = dataRoot });
            await DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_EMULATORLOCATION, value = emulator });

            if (!string.IsNullOrEmpty(inp_Password.Text))
            {
                await DatabaseHandler.InsertIntoTable(new dbo_Config() { key = MainWindow.CONFIG_PASSWORD, value = EncryptionHelper.EncryptPassword(inp_Password.Text) });
            }

            MainWindow.window!.CheckForSetup();
        }

        private bool CanContinue()
        {
            string password = inp_Password.Text;

            if (!string.IsNullOrEmpty(dataRoot) && !string.IsNullOrEmpty(emulator))
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

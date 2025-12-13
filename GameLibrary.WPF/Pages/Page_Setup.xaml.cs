using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

            inp_Emulator.Content = MainWindow.EmulatorLocation ?? "Set Location";
        }

        private async void btn_Complete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanContinue())
                return;

            await ConfigHandler.SaveConfigValue(ConfigHandler.ConfigValues.RootPath, dataRoot);
            await ConfigHandler.SaveConfigValue(ConfigHandler.ConfigValues.EmulatorPath, emulator);

            if (!string.IsNullOrEmpty(inp_Password.Text))
            {
                await ConfigHandler.SaveConfigValue(ConfigHandler.ConfigValues.PasswordHash, EncryptionHelper.EncryptPassword(inp_Password.Text));
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

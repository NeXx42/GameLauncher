using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System.Windows;
using System.Windows.Controls;

namespace GameLibary.Pages
{
    /// <summary>
    /// Interaction logic for Page_Lock.xaml
    /// </summary>
    public partial class Page_Lock : Page
    {
        public Page_Lock()
        {
            InitializeComponent();
            btn_login.Click += async (_, __) => await AttemptLogin();
        }

        public async Task AttemptLogin()
        {
            dbo_Config? password = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.PasswordHash);
            string testPassword = inp_password.Text;

            if (EncryptionHelper.TestPassword(testPassword, password?.value))
            {
                MainWindow.window!.LoadPage<Page_Content>();
            }
            else
            {
                inp_password.Text = "";
                MessageBox.Show("Incorrect password");
            }
        }
    }
}

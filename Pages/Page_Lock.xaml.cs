using GameLibary.Source.Database.Tables;
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
            btn_login.Click += (_, __) => AttemptLogin();
        }

        public void AttemptLogin()
        {
            dbo_Config? password = DatabaseHandler.GetItems<dbo_Config>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Config.key), MainWindow.CONFIG_PASSWORD)).FirstOrDefault();
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

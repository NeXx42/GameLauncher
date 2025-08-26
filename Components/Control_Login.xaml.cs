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
using GameLibary.Source;
using GameLibary.Source.Database.Tables;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_Login.xaml
    /// </summary>
    public partial class Control_Login : UserControl
    {
        public Control_Login()
        {
            InitializeComponent();
            btn_login.Click += (_, __) => AttemptLogin();
        }

        public void AttemptLogin()
        {
            dbo_Config? password = DatabaseHandler.GetItems<dbo_Config>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Config.key), MainWindow.CONFIG_PASSWORD)).FirstOrDefault();
            string testPassword = inp_password.Text;

            if(EncryptionHelper.TestPassword(testPassword, password?.value))
            {
                MainWindow.window.CompleteLoad();
            }
            else
            {
                inp_password.Text = "";
                MessageBox.Show("Incorrect password");
            }
        }
    }
}

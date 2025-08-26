using GameLibary.Source;
using GameLibary.Source.Database.Tables;
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

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_CreateTag.xaml
    /// </summary>
    public partial class Control_CreateTag : UserControl
    {
        public Control_CreateTag()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(inp_TagName.Text))
                return;

            DatabaseHandler.InsertIntoTable(new dbo_Tag()
            {
                TagName = inp_TagName.Text,
            });

            LibaryHandler.MarkTagsAsDirty();

            MainWindow.window.DrawTags();
            MainWindow.window.ToggleMenu(false);
        }
    }
}

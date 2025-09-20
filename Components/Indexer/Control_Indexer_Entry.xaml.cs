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

namespace GameLibary.Components.Indexer
{
    /// <summary>
    /// Interaction logic for Control_Indexer_Entry.xaml
    /// </summary>
    public partial class Control_Indexer_Entry : UserControl
    {
        public Control_Indexer_Entry()
        {
            InitializeComponent();
        }

        public void Draw(Control_Indexer.GameFolder folder)
        {
            loc.Content = folder.path;
        }
    }
}

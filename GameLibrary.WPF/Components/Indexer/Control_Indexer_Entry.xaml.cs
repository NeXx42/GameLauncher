using System.Windows.Controls;

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

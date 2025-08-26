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
using GameLibary.Source.Database.Tables;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Element_Tag.xaml
    /// </summary>
    public partial class Element_Tag : UserControl
    {
        private Action clickEvent;

        public Element_Tag()
        {
            InitializeComponent();
            border.MouseLeftButtonDown += (_, __) => clickEvent?.Invoke();

            this.Width = 150;
            this.Height = 30;

            this.Margin = new Thickness(0, 0, 5, 5);
        }

        public void Draw(dbo_Tag tag, Action<int> onClick)
        {
            Toggle(false);

            clickEvent = () => onClick?.Invoke(tag.TagId);
            txt.Content = tag.TagName;
        }

        public void Toggle(bool to)
        {
            border.BorderThickness = new Thickness(to ? 2 : 0);
        }
    }
}

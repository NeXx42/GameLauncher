using GameLibary.Source.Database.Tables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            border.Margin = new Thickness(0, 0, 5, 5);

            this.Width = 150;
            this.Height = 40;
        }

        public void Draw(dbo_Tag tag, Action<int> onClick)
        {
            Toggle(false);

            clickEvent = () => onClick?.Invoke(tag.TagId);
            txt.Content = tag.TagName;
        }

        public void Toggle(bool to)
        {
            border.Background = to ? new SolidColorBrush(Color.FromRgb(54, 94, 144)) : new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
    }
}

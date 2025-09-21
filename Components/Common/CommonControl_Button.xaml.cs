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

namespace GameLibary.Components.Common
{
    /// <summary>
    /// Interaction logic for CommonControl_Button.xaml
    /// </summary>
    public partial class CommonControl_Button : UserControl
    {
        public CommonControl_Button()
        {
            InitializeComponent();
        }

        // Define a dependency property
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(CommonControl_Button), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public void RegisterClick(Action callback)
        {
            this.ctrl.MouseLeftButtonDown += (_, __) => callback?.Invoke();
        }
    }
}

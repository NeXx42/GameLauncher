using System.Windows;
using System.Windows.Controls;

namespace GameLibary.Components.Common
{
    /// <summary>
    /// Interaction logic for CommonControl_Toggle.xaml
    /// </summary>
    public partial class CommonControl_Toggle : UserControl
    {
        private bool isOn;
        public bool getIsOn => isOn;

        private Action<bool> onCallback;


        public CommonControl_Toggle()
        {
            InitializeComponent();

            control.MouseLeftButtonDown += (_, __) => Toggle(!isOn);
            Toggle(false);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(CommonControl_Toggle), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public void Toggle(bool to)
        {
            ToggleSilent(to);
            onCallback?.Invoke(isOn);
        }

        public void ToggleSilent(bool to)
        {
            isOn = to;
            RedrawContent();
        }


        private void RedrawContent()
        {
            opt_Disabled.Visibility = isOn ? Visibility.Hidden : Visibility.Visible;
            opt_Enabled.Visibility = !isOn ? Visibility.Hidden : Visibility.Visible;
        }


        public void RegisterOnChange(Action<bool> callback)
        {
            onCallback = callback;
        }
    }
}

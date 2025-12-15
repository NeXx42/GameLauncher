using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace GameLibrary.Avalonia.Controls
{
    public partial class Common_Button : UserControl
    {
        public Common_Button()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<Common_Button, string>(nameof(Label), string.Empty);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public void RegisterClick(Func<Task> callback)
        {
            ctrl.PointerPressed += async (_, __) => await callback();
        }

        public void RegisterClick(Action callback)
        {
            ctrl.PointerPressed += (_, __) => callback?.Invoke();
        }
    }
}

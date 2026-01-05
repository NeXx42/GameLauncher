using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;

namespace GameLibrary.AvaloniaUI.Controls
{
    public partial class Common_Button : UserControl
    {
        private Action? callback;
        private string? defaultMessage;

        public Common_Button()
        {
            InitializeComponent();
            DataContext = this;

            ctrl.PointerPressed += (_, __) => callback?.Invoke();
        }

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<Common_Button, string>(nameof(Label), string.Empty);

        public string Label
        {
            get => GetValue(LabelProperty);
            set
            {
                defaultMessage = value.ToUpper();
                SetValue(LabelProperty, value.ToUpper());
            }
        }

        public void RegisterClick(Func<Task> callback, string? asyncMessage = "")
        {
            this.callback += async () => await HandleUpdate();

            async Task HandleUpdate()
            {
                if (!string.IsNullOrEmpty(asyncMessage))
                    Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, asyncMessage));

                await callback();

                if (!string.IsNullOrEmpty(asyncMessage))
                    Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, defaultMessage));
            }
        }

        public void RegisterClick(Action callback)
        {
            this.callback += () => callback?.Invoke();
        }
    }
}

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

namespace GameLibrary.Avalonia.Controls
{
    public partial class Common_Button : UserControl
    {
        private Action callback;

        private ImmutableSolidColorBrush? originalBrush;
        private ImmutableSolidColorBrush? selectedBrush;

        private CancellationTokenSource animationToken;

        public Common_Button()
        {
            InitializeComponent();
            DataContext = this;

            if (ctrl.Background is ImmutableSolidColorBrush scb)
            {
                callback = AnimatePress;

                float mixAmount = .2f;

                originalBrush = scb;
                selectedBrush = new ImmutableSolidColorBrush(Color.FromArgb(
                    originalBrush.Color.A,
                    (byte)(originalBrush.Color.R + (255 - originalBrush.Color.R) * mixAmount),
                    (byte)(originalBrush.Color.G + (255 - originalBrush.Color.G) * mixAmount),
                    (byte)(originalBrush.Color.B + (255 - originalBrush.Color.B) * mixAmount)
                ));
            }

            ctrl.PointerPressed += (_, __) => callback?.Invoke();
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
            this.callback += async () => await callback();
        }

        public void RegisterClick(Action callback)
        {
            this.callback += () => callback?.Invoke();
        }

        private async void AnimatePress()
        {
            animationToken?.Cancel();
            animationToken = new CancellationTokenSource();

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(Border.BackgroundProperty, originalBrush) },
                        Cue = new Cue(0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Border.BackgroundProperty, selectedBrush) },
                        Cue = new Cue(0.5)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Border.BackgroundProperty, originalBrush) },
                        Cue = new Cue(1)
                    }
                }
            };

            await animation.RunAsync(ctrl, animationToken.Token);
        }
    }
}

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.AvaloniaUI.Controls;

public partial class Common_InputField : UserControl
{
    public static readonly StyledProperty<string> PlaceholderProperty = AvaloniaProperty.Register<Common_InputField, string>(nameof(Placeholder), string.Empty);

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value.ToUpper());
    }

    private Action? onTextChange;

    public string? getText => inp.Text;

    public Common_InputField()
    {
        InitializeComponent();
        DataContext = this;

        inp.KeyUp += (_, __) => onTextChange?.Invoke();
    }

    public void OnChange(Func<Task> callback)
    {
        onTextChange = () => _ = callback();
    }

    public void OnChange(Action callback)
    {
        onTextChange = callback;
    }
}
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace GameLibrary.AvaloniaUI.Controls;

public partial class Common_ButtonToggle : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<Common_ButtonToggle, string>(nameof(Label), string.Empty);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> InternalPaddingProperty = AvaloniaProperty.Register<Common_ButtonToggle, string>(nameof(InternalPadding), "10 2");

    public string InternalPadding
    {
        get => GetValue(InternalPaddingProperty);
        set => SetValue(InternalPaddingProperty, value);
    }

    private static SolidColorBrush? m_Brush_Selected;
    private static SolidColorBrush Brush_Selected
    {
        get
        {
            m_Brush_Selected ??= new SolidColorBrush(Color.FromRgb(26, 159, 255));
            return m_Brush_Selected;
        }
    }

    private static SolidColorBrush? m_Brush_UnSelected;
    private static SolidColorBrush Brush_UnSelected
    {
        get
        {
            m_Brush_UnSelected ??= new SolidColorBrush(Color.FromRgb(20, 30, 40));
            return m_Brush_UnSelected;
        }
    }

    private static SolidColorBrush? m_BorderBrush_UnSelected;
    private static SolidColorBrush BorderBrush_UnSelected
    {
        get
        {
            m_BorderBrush_UnSelected ??= new SolidColorBrush(Color.FromRgb(38, 43, 48));
            return m_BorderBrush_UnSelected;
        }
    }

    public bool isSelected
    {
        set
        {
            ctrl.Background = value ? Brush_Selected : Brush_UnSelected;
            ctrl.BorderBrush = value ? Brush_Selected : BorderBrush_UnSelected;

            m_isSelected = value;
        }
        get => m_isSelected;
    }
    private bool m_isSelected;

    private Action<bool>? callback;

    public bool autoToggle = true;

    public Common_ButtonToggle()
    {
        InitializeComponent();
        DataContext = this;

        isSelected = false;
        ctrl.PointerPressed += (_, __) => HandleToggle();
    }

    public void Register(Action<bool> onToggle)
    {
        callback = onToggle;
    }

    public void Register(Func<bool, Task> onToggle, string? asyncMessage)
    {
        callback = async (b) => await onToggle(b);
    }

    private void HandleToggle()
    {
        if (autoToggle)
        {
            isSelected = !isSelected;
        }

        callback?.Invoke(isSelected);
    }
}
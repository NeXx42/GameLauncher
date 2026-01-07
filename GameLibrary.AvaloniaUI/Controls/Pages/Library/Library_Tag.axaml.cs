using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_Tag : UserControl
{
    #region  brush
    private static SolidColorBrush? m_Brush_Selected;
    private static SolidColorBrush Brush_Selected
    {
        get
        {
            m_Brush_Selected ??= new SolidColorBrush(Color.FromArgb(51, 26, 159, 255));
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
    #endregion

    #region border
    private static SolidColorBrush? m_Border_Selected;
    private static SolidColorBrush Border_Selected
    {
        get
        {
            m_Border_Selected ??= new SolidColorBrush(Color.FromRgb(26, 159, 255));
            return m_Border_Selected;
        }
    }

    private static SolidColorBrush? m_Border_UnSelected;
    private static SolidColorBrush Border_UnSelected
    {
        get
        {
            m_Border_UnSelected ??= new SolidColorBrush(Color.FromRgb(41, 48, 54));
            return m_Border_UnSelected;
        }
    }
    #endregion

    #region shadow
    private static BoxShadows? m_Shadow_Selected;
    private static BoxShadows Shadow_Selected
    {
        get
        {
            m_Shadow_Selected ??= new BoxShadows(new BoxShadow()
            {

            });

            return m_Shadow_Selected.Value;
        }
    }

    private static BoxShadows? m_Shadow_UnSelected;
    private static BoxShadows Shadow_UnSelected
    {
        get
        {
            m_Shadow_UnSelected ??= new BoxShadows();
            return m_Shadow_UnSelected.Value;
        }
    }
    #endregion

    private Action? clickEvent;

    public Library_Tag()
    {
        InitializeComponent();
        border.PointerPressed += (_, __) => clickEvent?.Invoke();
        //border.Margin = new Thickness(0, 0, 5, 5);
    }

    public void Draw(TagDto tag, Action<TagDto>? onClick)
    {
        Toggle(false);

        clickEvent = () => onClick?.Invoke(tag);
        DrawName(tag);
    }

    public void DrawName(TagDto tag)
    {
        txt.Text = tag.name.Replace("\n", "");
    }

    public void Toggle(bool to)
    {
        border.BorderBrush = to ? Border_Selected : Border_UnSelected;
        border.Background = to ? Brush_Selected : Brush_UnSelected;
        border.BoxShadow = to ? Shadow_Selected : Shadow_UnSelected;
    }
}
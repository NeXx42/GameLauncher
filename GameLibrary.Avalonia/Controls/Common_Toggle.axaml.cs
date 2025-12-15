using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.Avalonia.Controls;

public partial class Common_Toggle : UserControl
{
    private bool activeValue;
    private Action<bool> listeningEvent;

    public Common_Toggle()
    {
        InitializeComponent();
        inp.IsCheckedChanged += (_, __) => ChangeCallback();
    }

    public void SilentSetValue(bool to)
    {
        activeValue = to;
        inp.IsChecked = to;
    }

    public void RegisterOnChange(Action<bool> onChange)
    {
        listeningEvent += onChange;
    }

    private void ChangeCallback()
    {
        activeValue = inp.IsChecked ?? false;
        listeningEvent?.Invoke(activeValue);
    }
}
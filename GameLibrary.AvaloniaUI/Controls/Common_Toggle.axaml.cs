using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.AvaloniaUI.Controls;

public partial class Common_Toggle : UserControl
{
    private bool activeValue;

    private bool ignoreEvents;
    private Action<bool> listeningEvent;

    public Common_Toggle()
    {
        InitializeComponent();
        inp.IsCheckedChanged += (_, __) => ChangeCallback();
    }

    public void SilentSetValue(bool to)
    {
        ignoreEvents = true;
        activeValue = to;
        inp.IsChecked = to;
        ignoreEvents = false;
    }

    public void RegisterOnChange(Func<bool, Task> onChange)
    {
        listeningEvent += async (x) => await onChange(x);
    }

    public void RegisterOnChange(Action<bool> onChange)
    {
        listeningEvent += onChange;
    }

    private void ChangeCallback()
    {
        if (ignoreEvents)
            return;

        activeValue = inp.IsChecked ?? false;
        listeningEvent?.Invoke(activeValue);
    }
}
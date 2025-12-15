using System;
using System.Collections;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GameLibrary.Avalonia.Controls;

public partial class Common_Dropdown : UserControl
{
    private bool ignoreEvents;
    private Action selectionChangeCallback;

    public int selectedIndex => inp.SelectedIndex;
    public object selectedValue => inp.SelectedValue;


    public Common_Dropdown()
    {
        InitializeComponent();

        ignoreEvents = false;
        inp.SelectionChanged += (_, __) => OnChangeCallback();
    }

    public void SetupAsync(IEnumerable collection, int? defaultOption, Func<Task> onChange)
        => Setup(collection, defaultOption, async () => await onChange());

    public void Setup(IEnumerable collection, int? defaultOption, Action onChange)
    {
        ignoreEvents = true;
        inp.ItemsSource = collection;
        selectionChangeCallback = onChange;

        if (defaultOption.HasValue)
            SilentlyChangeValue(defaultOption.Value);

        ignoreEvents = false;
    }

    private void OnChangeCallback()
    {
        if (ignoreEvents)
            return;

        selectionChangeCallback?.Invoke();
    }

    public void SilentlyChangeValue(int index)
    {
        ignoreEvents = true;
        inp.SelectedIndex = index;
        ignoreEvents = false;
    }

    private void ToggleDropdown(object sender, RoutedEventArgs e)
    {
        inp.IsDropDownOpen = true;
    }
}
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace GameLibary.Components.Common
{
    /// <summary>
    /// Interaction logic for CommonControl_Dropdown.xaml
    /// </summary>
    public partial class CommonControl_Dropdown : UserControl
    {
        private bool ignoreEvents;
        private Action selectionChangeCallback;

        public int selectedIndex => inp.SelectedIndex;
        public object selectedValue => inp.SelectedValue;


        public CommonControl_Dropdown()
        {
            InitializeComponent();

            ignoreEvents = false;
            inp.SelectionChanged += (_, __) => OnChangeCallback();
        }

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
}

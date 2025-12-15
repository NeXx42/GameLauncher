using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.Avalonia.Windows;

public partial class Window_Dialog : Window
{
    private bool? clickedButton;
    public bool? didSelectPositive => clickedButton;

    public Window_Dialog()
    {
        InitializeComponent();

        btn_Positive.RegisterClick(() => OnSelectOption(true));
        btn_Negative.RegisterClick(() => OnSelectOption(false));
    }

    public void Setup(string header, string description, string positiveButton, string negativeButton)
    {
        clickedButton = null;

        this.Title = header;

        lbl_Description.Text = description;
        btn_Positive.Label = positiveButton;
        btn_Negative.Label = negativeButton;
    }

    private void OnSelectOption(bool option)
    {
        clickedButton = option;
        this.Close();
    }
}
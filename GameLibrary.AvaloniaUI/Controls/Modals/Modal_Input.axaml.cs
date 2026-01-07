using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.AvaloniaUI.Controls.Modals;

public partial class Modal_Input : UserControl
{

    private TaskCompletionSource<string?>? stringRequest;

    public Modal_Input()
    {
        InitializeComponent();
        btn_Enter.RegisterClick(CompleteInput);
        btn_Close.RegisterClick(() => stringRequest?.SetResult(null));
    }

    public Task<string?> RequestStringAsync(string windowName, string? existingText, bool obfuscateInput = false)
    {
        lbl_Title.Content = windowName;

        txt_Input.Text = existingText;
        txt_Input.PasswordChar = obfuscateInput ? '*' : '\0';

        stringRequest = new TaskCompletionSource<string?>();
        return stringRequest.Task;
    }

    private void CompleteInput()
    {
        stringRequest?.SetResult(txt_Input.Text);
    }
}
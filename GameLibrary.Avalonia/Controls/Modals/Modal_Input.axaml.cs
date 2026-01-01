using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.Avalonia.Controls.Modals;

public partial class Modal_Input : UserControl
{

    private TaskCompletionSource<string?>? stringRequest;

    public Modal_Input()
    {
        InitializeComponent();
        btn_Enter.RegisterClick(CompleteInput);
        btn_Close.RegisterClick(() => stringRequest?.SetResult(null));
    }

    public Task<string?> RequestString(string windowName, string? existingText)
    {
        lbl_Title.Content = windowName;
        txt_Input.Text = existingText;

        stringRequest = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return stringRequest.Task;
    }

    private void CompleteInput()
    {
        stringRequest?.SetResult(txt_Input.Text);
    }
}
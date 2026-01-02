using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.Avalonia.Controls.Modals;

public partial class Modal_YesNo : UserControl
{
    private TaskCompletionSource<bool>? boolResponse;
    private (Func<Task>, string?)? asyncModalOptions;


    public Modal_YesNo()
    {
        InitializeComponent();

        btn_Negative.RegisterClick(() => boolResponse?.SetResult(false));
    }

    public Task<bool> RequestModal(string title, string paragraph)
    {
        boolResponse = new TaskCompletionSource<bool>();
        btn_Positive.RegisterClick(() => boolResponse?.SetResult(true));

        lbl_Title.Content = title;
        lbl_Paragraph.Text = paragraph;

        return boolResponse.Task;
    }

    public Task<bool> RequestModal(string title, string paragraph, Func<Task> positiveCallback, string? loadingMessage)
    {
        boolResponse = new TaskCompletionSource<bool>();
        btn_Positive.RegisterClick(OnPositiveClick, loadingMessage);

        lbl_Title.Content = title;
        lbl_Paragraph.Text = paragraph;

        return boolResponse.Task;

        async Task OnPositiveClick()
        {
            await positiveCallback();
            boolResponse.SetResult(true);
        }
    }
}
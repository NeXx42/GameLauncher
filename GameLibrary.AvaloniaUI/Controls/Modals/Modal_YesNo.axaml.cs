using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameLibrary.AvaloniaUI.Controls.Modals;

public partial class Modal_YesNo : UserControl
{
    private TaskCompletionSource<int>? completeResponse;

    public Modal_YesNo()
    {
        InitializeComponent();

        btn_Negative.RegisterClick(() => completeResponse?.SetResult(-1));
    }

    public Task<int> RequestModal(string title, string paragraph)
    {
        completeResponse = new TaskCompletionSource<int>();
        DrawGenericDetails(title, paragraph, ("Yes", () => Task.CompletedTask, "Loading"));

        return completeResponse.Task;
    }

    public Task<int> RequestGeneric(string title, string paragraph, params (string btnText, Func<Task> callback, string? loadingMessage)[] btns)
    {
        completeResponse = new TaskCompletionSource<int>();
        DrawGenericDetails(title, paragraph, btns);

        return completeResponse.Task;
    }

    private void DrawGenericDetails(string title, string paragraph, params (string btnText, Func<Task> callback, string? loadingMessage)[]? btns)
    {
        lbl_Title.Content = title;
        lbl_Paragraph.Text = paragraph;

        btn_Negative.Label = btns?.Length == 0 ? "Ok" : "No";

        extraBtns.Children.Clear();

        if (btns != null)
        {
            for (int i = 0; i < btns.Length; i++)
            {
                int callbackId = i;

                Common_Button btnUI = new Common_Button();

                btnUI.MinWidth = btn_Negative.Width;
                btnUI.Label = btns[callbackId].btnText;
                btnUI.RegisterClick(() => GenericCallback(callbackId), btns[callbackId].loadingMessage);

                extraBtns.Children.Add(btnUI);
            }
        }

        async Task GenericCallback(int btnId)
        {
            await btns[btnId].callback();
            completeResponse?.SetResult(btnId);
        }
    }
}
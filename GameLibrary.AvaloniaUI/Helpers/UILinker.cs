using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Controls.Modals;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.AvaloniaUI.Helpers;

public class UILinker : IUILinker
{
    public async Task OpenLoadingModal(bool progressiveLoad, Func<Task>[] tasks)
    {
        await MainWindow.instance!.DisplayModal<Modal_Loading>(ModalRequest);

        async Task ModalRequest(Modal_Loading modal)
        {
            await modal.LoadTasks(progressiveLoad, tasks);
        }
    }

    public async Task<string?> OpenStringInputModal(string windowName, string? existingText = "")
    {
        string? res = null;
        await MainWindow.instance!.DisplayModal<Modal_Input>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_Input modal)
        {
            res = await modal.RequestString(windowName, existingText);
        }
    }

    public async Task<bool> OpenYesNoModal(string title, string paragraph)
    {
        bool res = false;
        await MainWindow.instance!.DisplayModal<Modal_YesNo>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_YesNo modal)
        {
            res = (await modal.RequestModal(title, paragraph)) != -1;
        }
    }

    public async Task<bool> OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string? loadingMessage)
    {
        bool res = false;
        await MainWindow.instance!.DisplayModal<Modal_YesNo>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_YesNo modal)
        {
            res = (await modal.RequestGeneric(title, paragraph, ("Yes", positiveCallback, loadingMessage))) != -1;
        }
    }

    public async Task<int> OpenConfirmationAsync(string title, string paragraph, params (string btn, Func<Task> callback, string? loadingMessage)[] controls)
    {
        int res = -1;
        await MainWindow.instance!.DisplayModal<Modal_YesNo>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_YesNo modal)
        {
            res = await modal.RequestGeneric(title, paragraph, controls);
        }
    }

    public void InvokeOnUIThread(Action a) => Dispatcher.UIThread.Post(a);
}

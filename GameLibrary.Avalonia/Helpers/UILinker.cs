using System;
using System.Threading.Tasks;
using GameLibrary.Avalonia.Controls.Modals;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.Avalonia.Helpers;

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
            res = await modal.RequestModal(title, paragraph);
        }
    }

    public async Task OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string? loadingMessage)
    {
        await MainWindow.instance!.DisplayModal<Modal_YesNo>(ModalRequest);

        async Task ModalRequest(Modal_YesNo modal)
        {
            await modal.RequestModal(title, paragraph, positiveCallback, loadingMessage);
        }
    }
}

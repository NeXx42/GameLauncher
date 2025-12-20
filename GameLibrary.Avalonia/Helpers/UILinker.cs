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

    public async Task<string?> OpenStringInputModal(string windowName)
    {
        string? res = null;
        await MainWindow.instance!.DisplayModal<Modal_Input>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_Input modal)
        {
            res = await modal.RequestString(windowName);
        }
    }
}

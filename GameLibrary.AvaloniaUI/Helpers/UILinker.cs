using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Controls.Modals;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.AvaloniaUI.Helpers;

public class UILinker : IUILinker
{
    public void Quit()
    {
        MainWindow.instance!.Close();
    }

    public async Task OpenLoadingModal(bool progressiveLoad, Func<Task>[] tasks)
    {
        await MainWindow.instance!.DisplayModal<Modal_Loading>(ModalRequest);

        async Task ModalRequest(Modal_Loading modal)
        {
            await modal.LoadTasks(progressiveLoad, tasks);
        }
    }

    public async Task<string?> OpenStringInputModal(string windowName, string? existingText = "", bool obfuscateInput = false)
    {
        string? res = null;
        await MainWindow.instance!.DisplayModal<Modal_Input>(ModalRequest);

        return res;

        async Task ModalRequest(Modal_Input modal)
        {
            res = await modal.RequestString(windowName, existingText, obfuscateInput);
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

    public async Task<string[]?> OpenFoldersDialog(string title)
    {
        var res = await MainWindow.instance!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = true,
            Title = title
        });

        if (res.Count == 0)
            return null;

        return res.Select(x => x.Path.AbsolutePath).ToArray();
    }

    public async Task<string?> OpenFolderDialog(string title)
    {
        var res = await OpenFoldersDialog(title);
        return res != null ? res[0] : null;
    }

    public async Task<string[]?> OpenFilesDialog(string title, string[] allowedTypes)
    {
        FilePickerFileType[]? types = null;

        if (allowedTypes.Length > 0)
            types = allowedTypes.Select(x => new FilePickerFileType(x) { Patterns = [x] }).ToArray();

        var res = await MainWindow.instance!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = true,
            FileTypeFilter = types,
            Title = title
        });

        if (res.Count == 0)
            return null;

        return res.Select(x => x.Path.AbsolutePath).ToArray();
    }

    public async Task<string?> OpenFileDialog(string title, string[] allowedTypes)
    {
        var res = await OpenFilesDialog(title, allowedTypes);
        return res != null ? res[0] : null;
    }
}

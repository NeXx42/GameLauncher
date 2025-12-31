using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Controls.Windows;

namespace GameLibrary.Avalonia.Helpers;

public static class DialogHelper
{
    public static async Task<IReadOnlyList<IStorageFolder>> OpenFolderAsync(FolderPickerOpenOptions options)
    {
        return await MainWindow.instance!.StorageProvider.OpenFolderPickerAsync(options);
    }

    public static async Task<T> OpenDialog<T>(Func<T, Task> setup) where T : Window
    {
        T dialog = Activator.CreateInstance<T>();

        await setup(dialog);
        await dialog.ShowDialog<T>(MainWindow.instance!);

        return dialog;
    }

    public static async Task<bool> OpenDialog(string header, string description, string positiveButton, string? negativeButton)
    {
        Window_Dialog dialog = new Window_Dialog();

        dialog.Setup(header, description, positiveButton, negativeButton);
        await dialog.ShowDialog<Window_Dialog>(MainWindow.instance!);

        return dialog.didSelectPositive ?? false;
    }

    public static async Task OpenDatabaseExceptionDialog(Exception e, string sql)
    {
        await OpenDialog("Exception", $"{sql}\n\n{e.Message}", "Ok", null);
    }


    public static async Task OpenExceptionDialog(Exception msg)
    {
        await OpenDialog("Exception", msg.Message, "Ok", null);
    }

    public static async Task OpenOverlay(int gameId)
    {
        //OverlayUI overlay = new OverlayUI();
        //overlay.Show();
    }
}

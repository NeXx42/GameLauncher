using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Windows;

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

    public static async Task<bool> OpenDialog(string header, string description, string positiveButton, string negativeButton)
    {
        Window_Dialog dialog = new Window_Dialog();

        dialog.Setup(header, description, positiveButton, negativeButton);
        await dialog.ShowDialog<Window_Dialog>(MainWindow.instance!);

        return dialog.didSelectPositive ?? false;
    }

    public static void OpenExceptionDialog(Exception msg)
    {

    }
}

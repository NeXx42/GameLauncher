using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Controls;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Avalonia.Pages.Indexer;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_AddGames : UserControl
{
    public Func<Task>? onReimportGames;

    public Popup_AddGames()
    {
        InitializeComponent();

        cont_ImportView.Setup(this);
        cont_FolderView.Setup(this);
    }


    public void Setup(Func<Task> onReimport)
    {
        onReimportGames = onReimport;
    }

    public async void OnOpen()
    {
        this.IsVisible = true;
        CloseFolderView();
    }

    public void RequestFolderView(FileManager.FolderEntry folder, Indexer_Folder ui)
    {
        cont_ImportView.IsVisible = false;
        cont_FolderView.IsVisible = true;
        cont_FolderView.RequestFolderView(folder, ui);
    }

    public void CloseFolderView()
    {
        cont_ImportView.IsVisible = true;
        cont_FolderView.IsVisible = false;
    }
}
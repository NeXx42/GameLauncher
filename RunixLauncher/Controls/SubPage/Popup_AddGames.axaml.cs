using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.AvaloniaUI.Controls.SubPage.Indexer;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI.Controls.SubPage;

public partial class Popup_AddGames : UserControl
{
    public Func<Task>? onReimportGames;

    public Popup_AddGames()
    {
        InitializeComponent();

        _ = cont_ImportView.Setup(this);
        _ = cont_FolderView.Setup(this);
    }


    public void Setup(Func<Task> onReimport)
    {
        onReimportGames = onReimport;
    }

    public async Task OnOpen()
    {
        this.IsVisible = true;
        CloseFolderView();
    }

    public void RequestFolderView(FileManager.ImportEntry_Folder folder, Indexer_Folder ui)
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
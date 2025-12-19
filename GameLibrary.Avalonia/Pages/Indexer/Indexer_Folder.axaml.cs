using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Indexer;

public partial class Indexer_Folder : UserControl
{
    private FileManager.FolderEntry? folder;
    private Action<FileManager.FolderEntry, Indexer_Folder>? requestFolderView;

    public Indexer_Folder()
    {
        InitializeComponent();

        btn_Extract.RegisterClick(ExtractArchive);
        btn_Explore.RegisterClick(ExploreExtracted);
    }

    public void Draw(FileManager.FolderEntry folder, Action<FileManager.FolderEntry, Indexer_Folder> requestFolderView)
    {
        this.requestFolderView = requestFolderView;
        this.folder = folder;

        RedrawUI();
    }

    public void RedrawUI()
    {
        lbl_Extract.Content = folder?.archiveFile;
        lbl_Folder.Content = folder?.selectedBinary ?? folder?.extractedEntry;

        btn_Extract.IsVisible = string.IsNullOrEmpty(folder?.extractedEntry) && !string.IsNullOrEmpty(folder?.archiveFile);
        btn_Explore.IsVisible = !string.IsNullOrEmpty(folder?.extractedEntry);
    }

    private async Task ExtractArchive()
    {
        if (folder == null)
            return;

        string? result = await FileManager.ExtractFolder(folder.archiveFile ?? string.Empty);

        if (string.IsNullOrEmpty(result))
            return;

        folder.extractedEntry = result;
        folder.CrawlForExecutable(result);

        RedrawUI();
    }

    private async Task ExploreExtracted()
    {
        requestFolderView?.Invoke(folder!, this);
    }
}
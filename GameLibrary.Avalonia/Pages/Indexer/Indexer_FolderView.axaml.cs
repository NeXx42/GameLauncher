using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.Avalonia.Controls;
using GameLibrary.Avalonia.Pages.Library;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Indexer;

public partial class Indexer_FolderView : UserControl
{
    private Popup_AddGames master;

    private FileManager.FolderEntry inspectingFolder;
    private Indexer_Folder inspectingFolderUI;

    private List<Label>? drawnFiles = new List<Label>();
    private Action? btn_Generic_Callback;
    private Action? btn_Launch_Callback;

    public Indexer_FolderView()
    {
        InitializeComponent();
    }

    public void Setup(Popup_AddGames master)
    {
        this.master = master;

        btn_FolderView_Close.RegisterClick(master.CloseFolderView);
        btn_Generic.RegisterClick(() => btn_Generic_Callback?.Invoke());
        btn_Launch.RegisterClick(() => btn_Launch_Callback?.Invoke());
    }

    public void RequestFolderView(FileManager.FolderEntry folder, Indexer_Folder ui)
    {
        if (string.IsNullOrEmpty(folder.extractedEntry) || !Directory.Exists(folder.extractedEntry))
            return;

        inspectingFolder = folder;
        inspectingFolderUI = ui;

        UpdateFolderDir(folder.extractedEntry);
    }


    private void UpdateFolderDir(string path)
    {
        string[] dirs = Directory.GetDirectories(path);
        string[] files = Directory.GetFiles(path);

        cont_FolderView_URI.Children.Clear();
        cont_FolderView_Entries.Children.Clear();
        drawnFiles.Clear();

        Common_Button rootBtn = new Common_Button();
        rootBtn.Label = "/";

        rootBtn.RegisterClick(() => UpdateFolderDir(inspectingFolder.extractedEntry!));
        cont_FolderView_URI.Children.Add(rootBtn);

        if (path != inspectingFolder.extractedEntry!)
        {
            string localPath = path.Remove(0, inspectingFolder.extractedEntry!.Length);
            string[] portions = localPath.Split("/");

            string buildingLoc = inspectingFolder.extractedEntry!;

            foreach (string portion in portions)
            {
                if (string.IsNullOrEmpty(portion))
                    continue;

                buildingLoc += "/" + portion;
                string temp = buildingLoc;

                Common_Button btn = new Common_Button();
                btn.Label = portion;

                btn.RegisterClick(() => UpdateFolderDir(temp));
                cont_FolderView_URI.Children.Add(btn);
            }

            string previousPath = Directory.GetParent(path)!.FullName;
            CreateEntry("..", true, () => UpdateFolderDir(previousPath));
        }

        foreach (string dir in dirs)
            CreateEntry(dir, true, () => UpdateFolderDir(dir));

        foreach (string dir in files)
            CreateEntry(dir, false, () => SelectFile(dir));

        void CreateEntry(string dir, bool isFolder, Action? callback)
        {
            Control control;

            if (isFolder)
            {
                Border b = new Border();
                b.Background = new ImmutableSolidColorBrush(Color.FromRgb(255, 0, 0));
                b.CornerRadius = new CornerRadius(5);

                Label l = new Label();
                l.Content = dir;
                l.Margin = new Thickness(5);

                b.Child = l;
                control = b;
            }
            else
            {
                Label l = new Label();
                l.Content = dir;

                control = l;
            }

            if (callback != null)
                control.PointerPressed += (_, __) => callback();

            cont_FolderView_Entries.Children.Add(control);
        }
    }

    private void SelectFile(string dir)
    {
        btn_Generic_Callback = null;
        btn_Launch_Callback = null;

        btn_Generic.IsVisible = false;
        btn_Launch.IsVisible = false;

        if (FileManager.IsExecutable(dir))
        {
            btn_Launch.IsVisible = true;
            btn_Launch_Callback = async () => await LaunchSelectedBinary(dir);

            btn_Generic.IsVisible = true;
            btn_Generic.Label = "Select Binary";

            btn_Generic_Callback = () => UpdateSelectedFolderBinary(dir);
        }
        else if (FileManager.IsZip(dir))
        {
            btn_Generic.IsVisible = true;
            btn_Generic.Label = "Extract Here";

            btn_Generic_Callback = async () => await ExtractFile(dir);
        }
    }

    private async Task LaunchSelectedBinary(string dir)
    {
        await GameLauncher.LaunchGeneric(dir);
    }

    private void UpdateSelectedFolderBinary(string dir)
    {
        inspectingFolder.selectedBinary = dir;
        inspectingFolderUI.RedrawUI();

        master.CloseFolderView();
    }

    private async Task ExtractFile(string dir)
    {
        await FileManager.ExtractFolder(dir);
        UpdateFolderDir(Path.GetDirectoryName(dir));
    }
}
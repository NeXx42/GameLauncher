using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Controls.Pages;

public partial class Page_Setup : UserControl
{
    private Action<SetupRequest>? setupCallback;

    private string? selectedDBFolder;
    private string? selectedLibraryStorageLocation;
    private string? enteredPin;

    public Page_Setup()
    {
        InitializeComponent();
        Revalidate();
    }

    public void Enter(Action<SetupRequest> onSetup)
    {
        setupCallback = onSetup;

        btn_DBLocation.RegisterClick(SelectDBLocation);
        btn_LibraryLocation.RegisterClick(SelectLibraryFolder);

        btn_Save.RegisterClick(CompleteSave);
    }

    private async void SelectDBLocation()
    {
        IReadOnlyList<IStorageFolder> selectedFolders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
        {
            Title = "DB Location",
            AllowMultiple = false
        });

        if (selectedFolders.Count == 1)
        {
            string path = selectedFolders[0].Path.AbsolutePath;
            string? existingDB = await CheckForExistingDB(path);

            if (!string.IsNullOrEmpty(existingDB))
            {
                if (await DialogHelper.OpenDialog(
                    "Use existing DB?",
                    $"Do you want to use the existing DB file {existingDB}?",
                    "Yes",
                    "No"))
                {
                    await DatabaseManager.CreateDBPointerFile(existingDB);
                    setupCallback?.Invoke(new SetupRequest(existingDB));

                    return;
                }
            }

            selectedDBFolder = path;
            Revalidate();
        }
    }

    private async Task<string?> CheckForExistingDB(string folder)
    {
        string[] existingDBS = Directory.GetFiles(folder).Where(x => x.EndsWith(".db")).ToArray();
        return existingDBS.Length > 0 ? existingDBS[0] : null;
    }


    private async void SelectLibraryFolder()
    {
        IReadOnlyList<IStorageFolder> selectedFolders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
        {
            Title = "DB Location",
            AllowMultiple = false
        });

        if (selectedFolders.Count == 1)
        {
            selectedLibraryStorageLocation = selectedFolders[0].Path.AbsolutePath;
            Revalidate();
        }
    }

    private void Revalidate()
    {
        btn_DBLocation.Label = selectedDBFolder ?? "Select Location";
        btn_LibraryLocation.Label = selectedLibraryStorageLocation ?? "Select Location";

        btn_Save.IsVisible = IsValidSelection();
    }

    public bool IsValidSelection() => !string.IsNullOrEmpty(selectedDBFolder)
        && !string.IsNullOrEmpty(selectedLibraryStorageLocation);


    public async void CompleteSave()
    {
        if (!IsValidSelection())
            return;

        string? password = string.IsNullOrEmpty(inp_Password.Text) ? "" : EncryptionHelper.EncryptPassword(inp_Password.Text);

        setupCallback?.Invoke(new SetupRequest()
        {
            isExistingLoad = false,

            dbFile = Path.Combine(selectedDBFolder!, $"{Guid.NewGuid()}.db"),
            libraryFolder = selectedLibraryStorageLocation!,
            pin = password
        });
    }

    public struct SetupRequest
    {
        public bool isExistingLoad;
        public string dbFile;
        public string libraryFolder;
        public string? pin;

        public SetupRequest(string existingDB)
        {
            isExistingLoad = true;
            dbFile = existingDB;
            libraryFolder = string.Empty;
            pin = null;
        }
    }
}
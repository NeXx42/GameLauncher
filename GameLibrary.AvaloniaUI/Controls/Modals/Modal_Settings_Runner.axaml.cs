using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.AvaloniaUI.Controls.Modals;

public partial class Modal_Settings_Runner : UserControl
{
    private TaskCompletionSource? modalRes;

    private int? selectedId;
    private string? selectedRoot;

    private string[]? versionOptions;

    private UITabGroup tabGroup;

    public Modal_Settings_Runner()
    {
        InitializeComponent();

        inp_Type.SetupAsync(System.Enum.GetNames(typeof(RunnerManager.RunnerType)), 0, UpdateVersionInput);

        btn_Close.RegisterClick(Close);
        btn_Save.RegisterClick(Save);

        btn_Dir.RegisterClick(SelectDirectory);
        btn_WineTricks.RegisterClick(OpenWineTricks, "Loading");

        tabGroup = new UITabGroup(TabGroup_Buttons, TabGroup_Content, true);
    }

    public Task HandleOpen(int? runnerId)
    {
        selectedId = runnerId;
        modalRes = new TaskCompletionSource();

        Draw(runnerId);

        return modalRes.Task;
    }

    private async void Draw(int? runnerId)
    {
        tabGroup.ChangeSelection(0);

        if (!runnerId.HasValue)
        {
            await UpdateVersionInput();
            return;
        }

        dbo_Runner? existing = await RunnerManager.GetRunnerProfile(runnerId.Value);
        dbo_RunnerConfig[] configValues = await RunnerManager.GetRunnerConfigValues(runnerId.Value);

        if (existing != null)
        {
            inp_Name.Text = existing.runnerName;
            inp_Type.ChangeValue(existing.runnerType);

            selectedRoot = existing.runnerRoot;
            btn_Dir.Label = selectedRoot;
        }
    }

    private void Close()
    {
        modalRes?.SetResult();
    }

    private async Task Save()
    {
        if (!ValidateInput())
            return;

        string version = versionOptions != null ? versionOptions[inp_Version.selectedIndex] : string.Empty;
        await RunnerManager.CreateProfile(selectedId, inp_Name.Text!, selectedRoot!, inp_Type.selectedIndex, version);
        modalRes?.SetResult();
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrEmpty(selectedRoot)) return false;
        if (string.IsNullOrEmpty(inp_Name.Text)) return false;

        return true;
    }

    private async Task SelectDirectory()
    {
        IReadOnlyList<IStorageFolder> folders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
        {
            Title = "Pick Folder",
            AllowMultiple = false,
        });

        if (folders.Count == 1)
        {
            selectedRoot = folders[0].Path.AbsolutePath;
            btn_Dir.Label = selectedRoot;
        }
    }

    private async Task UpdateVersionInput()
    {
        versionOptions = await RunnerManager.GetVersionsForRunnerTypes(inp_Type.selectedIndex);

        if (versionOptions != null)
        {
            inp_Version.IsVisible = true;
            inp_Version.Setup(versionOptions, 0, null);
        }
        else
        {
            inp_Version.IsVisible = false;
        }
    }

    private async Task OpenWineTricks()
    {
        await RunnerManager.RunWineTricks(selectedId.Value);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.Modals;

public partial class Modal_Settings_Runner : UserControl
{
    private TaskCompletionSource? modalRes;

    private RunnerDto? selectedRunner;
    private string? selectedRoot;

    private string[]? versionOptions;

    private UITabGroup tabGroup;

    private RunnerDto.RunnerType getCurrentRunnerType => selectedRunner?.runnerType ?? (RunnerDto.RunnerType)inp_Type.selectedIndex;


    public Modal_Settings_Runner()
    {
        InitializeComponent();

        btn_Close.RegisterClick(Close);
        btn_Save.RegisterClick(Save, "Saving");

        btn_Dir.RegisterClick(SelectDirectory);

        btn_Wine_WineCfg.RegisterClick(OpenWineTricks, "Loading");
        btn_Wine_SharedDocuments.Register(ShareDocuments, "Updating");

        tabGroup = new UITabGroup(TabGroup_Buttons, TabGroup_Content, true);
    }

    public Task HandleOpen(int? runnerId)
    {
        selectedRunner = null;
        modalRes = new TaskCompletionSource();

        Draw(runnerId);
        return modalRes.Task;
    }

    private async void Draw(int? runnerId)
    {
        selectedRunner = runnerId.HasValue ? RunnerManager.GetRunnerProfile(runnerId.Value) : null;

        string[] types = selectedRunner != null ? [selectedRunner.runnerType.ToString()] : System.Enum.GetNames(typeof(RunnerDto.RunnerType));
        inp_Type.SetupAsync(types, 0, ChangeRunnerType);

        await UpdateDefaultDetails();
        await UpdateWineDetails();

        await ChangeRunnerType();
        await tabGroup.ChangeSelection(0);
    }

    private async Task UpdateDefaultDetails()
    {
        inp_Name.Text = selectedRunner?.runnerName ?? string.Empty;

        selectedRoot = selectedRunner?.runnerRoot ?? string.Empty;
        btn_Dir.Label = selectedRoot ?? "Select directory";
    }

    private async Task UpdateWineDetails()
    {
        string? sharedDocuments = string.Empty;
        selectedRunner?.globalRunnerValues.TryGetValue(RunnerDto.RunnerConfigValues.Wine_SharedDocuments, out sharedDocuments);

        btn_Wine_SharedDocuments.Label = sharedDocuments ?? "Select folder";
        btn_Wine_SharedDocuments.isSelected = !string.IsNullOrEmpty(sharedDocuments);
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

        if (selectedRunner == null)
        {
            await RunnerManager.CreateProfile(inp_Name.Text!, selectedRoot!, inp_Type.selectedIndex, version);
        }
        else
        {
            selectedRunner.runnerName = inp_Name.Text!;
            selectedRunner.runnerRoot = selectedRoot!;
            selectedRunner.runnerVersion = version;

            await selectedRunner.UpdateDatabaseEntry();
        }

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
        string? folder = await DependencyManager.OpenFolderDialog("Pick folder");

        if (string.IsNullOrEmpty(folder))
            return;

        selectedRoot = folder;
        btn_Dir.Label = selectedRoot;
    }

    private async Task ChangeRunnerType()
    {
        switch (getCurrentRunnerType)
        {
            case RunnerDto.RunnerType.Wine:
            case RunnerDto.RunnerType.Wine_GE:
            case RunnerDto.RunnerType.umu_Launcher:
                tabGroup.ToggleGroupVisibility(1, true);
                break;

            default:
                tabGroup.ToggleGroupVisibility(1, false);
                break;
        }

        await UpdateVersionInput();
    }

    private async Task UpdateVersionInput()
    {
        versionOptions = await RunnerDto.GetVersionsForRunnerTypes((int)getCurrentRunnerType);

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

    private async Task<bool> EnsureExistingProfile()
    {
        if (selectedRunner == null)
        {
            await DependencyManager.OpenYesNoModal("Failed!", "Cannot complete operation on an uncreated profile.\nSave before attempting again.");
            return false;
        }

        return true;
    }

    private async Task OpenWineTricks()
    {
        if (!await EnsureExistingProfile())
            return;

        await RunnerManager.RunWineTricks(selectedRunner!.runnerId);
    }

    private async Task ShareDocuments(bool val)
    {
        if (!await EnsureExistingProfile())
            return;

        string? selectedPath = await DependencyManager.OpenFolderDialog("Select shared folder");

        if (string.IsNullOrEmpty(selectedPath))
            return;

        int result = await DependencyManager.OpenConfirmationAsync(
            "Share prefix documents?",
            "This is a destructive action,\nProceeding will delete the AppData, and Documents folder of the prefix and link to a shared directory.\nthis CAN NOT be undone!",
            ("Link", async () => await selectedRunner!.SharePrefixDocuments(selectedPath), "working")
        );

        if (result != -1)
        {
            await UpdateWineDetails();
        }
    }
}
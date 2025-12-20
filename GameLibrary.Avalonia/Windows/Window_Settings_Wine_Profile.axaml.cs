using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;

namespace GameLibrary.Avalonia.Windows;

public partial class Window_Settings_Wine_Profile : Window
{
    private dbo_WineProfile? inspectingProfile;

    public Window_Settings_Wine_Profile()
    {
        InitializeComponent();

        btn_Cancel.RegisterClick(OnCancel);
        btn_Save.RegisterClick(OnSave);

        btn_SelectDir.RegisterClick(SelectDirectory);
    }

    public void Setup(dbo_WineProfile? profile)
    {
        inspectingProfile = profile ?? new dbo_WineProfile() { id = -1, emulatorType = 0 };
        Redraw();
    }

    private void OnCancel()
    {
        this.Close();
    }

    private async Task OnSave()
    {
        if (IsValidSave())
        {
            inspectingProfile!.profileName = inp_Name.Text;
            await DatabaseHandler.AddOrUpdate(inspectingProfile!, QueryBuilder.SQLEquals(nameof(inspectingProfile.id), inspectingProfile!.id));

            this.Close();
        }
    }

    private void Redraw()
    {
        inp_Name.Text = inspectingProfile!.profileName;
        btn_SelectDir.Label = string.IsNullOrEmpty(inspectingProfile!.profileDirectory) ? "Select Location" : inspectingProfile!.profileDirectory;
    }

    private async Task SelectDirectory()
    {
        var folders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select Folder",
        });

        if (folders.Count == 1)
        {
            inspectingProfile!.profileDirectory = folders[0].Path.AbsolutePath;
            Redraw();
        }
    }

    private bool IsValidSave()
        => !string.IsNullOrEmpty(inp_Name.Text) && !string.IsNullOrEmpty(inspectingProfile?.profileDirectory);
}
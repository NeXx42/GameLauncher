using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Avalonia.Settings;
using GameLibrary.Avalonia.Windows;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Controls.Settings;

public partial class Control_Settings_Wine_Profiles : UserControl, ISettingControl
{
    private SettingBase? setting;

    private Border[]? profileUIS;
    private dbo_WineProfile[]? existingProfiles;

    private ImmutableSolidColorBrush selectedBrush;
    private ImmutableSolidColorBrush unselectedBrush;

    private int? selectedProfile
    {
        set
        {
            if (m_selectedProfile.HasValue)
            {
                profileUIS![m_selectedProfile.Value].Background = unselectedBrush;
                btn_Default.IsVisible = false;
            }

            m_selectedProfile = value;
            btn_Add.Label = value.HasValue ? "Edit" : "Add";

            if (m_selectedProfile.HasValue)
            {
                profileUIS![m_selectedProfile.Value].Background = selectedBrush;
                btn_Default.IsVisible = true;
            }
        }
        get => m_selectedProfile;
    }
    private int? m_selectedProfile;

    public Control_Settings_Wine_Profiles()
    {
        InitializeComponent();

        selectedBrush = new ImmutableSolidColorBrush(Color.FromRgb(0, 0, 0));
        unselectedBrush = new ImmutableSolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        selectedProfile = null;
        btn_Add.RegisterClick(OpenEditMenu);
        btn_Default.RegisterClick(MakeDefaultProfile);

        btn_Default.IsVisible = false;
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Wine_Profiles ui)
    {
        this.setting = setting;
        return this;
    }

    public async Task LoadValue()
    {
        existingProfiles = await setting!.LoadSetting<dbo_WineProfile[]>();
        profileUIS = new Border[existingProfiles?.Length ?? 0];

        container.Children.Clear();

        if (existingProfiles != null)
        {
            for (int i = 0; i < existingProfiles.Length; i++)
            {
                DrawProfile(i);
            }
        }
    }

    private void DrawProfile(int profileIndex)
    {
        Border grid = new Border();

        grid.CornerRadius = new CornerRadius(2);
        grid.Height = 30;
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.Background = unselectedBrush;

        Label l = new Label();
        l.Content = existingProfiles![profileIndex].profileName;
        l.VerticalAlignment = VerticalAlignment.Center;
        l.Margin = new Thickness(5);

        grid.Child = l;
        grid.PointerPressed += (_, __) => SelectProfile(profileIndex);

        container.Children.Add(grid);
        profileUIS![profileIndex] = grid;
    }

    private void SelectProfile(int profileId)
    {
        if (selectedProfile == profileId)
        {
            selectedProfile = null;
            return;
        }

        selectedProfile = profileId;
    }

    private async Task OpenEditMenu()
    {
        await DialogHelper.OpenDialog<Window_Settings_Wine_Profile>(SetupCall);

        Task SetupCall(Window_Settings_Wine_Profile dialog)
        {
            dialog.Setup(selectedProfile.HasValue ? existingProfiles![selectedProfile.Value] : null);
            return Task.CompletedTask;
        }
    }

    private async Task MakeDefaultProfile()
    {
        if (!selectedProfile.HasValue)
            return;

        dbo_WineProfile _temp = new dbo_WineProfile() { emulatorType = 0 };
        await DatabaseHandler.TryExecute($"UPDATE {_temp.tableName} SET {nameof(_temp.isDefault)} = {nameof(_temp.id)} = {existingProfiles![selectedProfile.Value].id}");
    }
}
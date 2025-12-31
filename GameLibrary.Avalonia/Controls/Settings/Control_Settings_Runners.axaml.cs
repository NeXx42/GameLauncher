using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.Avalonia.Controls.Modals;
using GameLibrary.Avalonia.Settings;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Controls.Settings;

public partial class Control_Settings_Runners : UserControl, ISettingControl
{
    private SettingBase? setting;

    private Border[]? profileUIS;
    private dbo_Runner[]? existingProfiles;

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

    public Control_Settings_Runners()
    {
        InitializeComponent();

        selectedBrush = new ImmutableSolidColorBrush(Color.FromRgb(0, 0, 0));
        unselectedBrush = new ImmutableSolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        selectedProfile = null;
        btn_Add.RegisterClick(OpenEditMenu);
        btn_Default.RegisterClick(MakeDefaultProfile);

        btn_Default.IsVisible = false;
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Runners ui)
    {
        this.setting = setting;
        return this;
    }

    public async Task LoadValue()
    {
        existingProfiles = await setting!.LoadSetting<dbo_Runner[]>();
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

    private void DrawProfile(int uiIndex)
    {
        Border grid = new Border();

        grid.CornerRadius = new CornerRadius(2);
        grid.Height = 30;
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.Background = unselectedBrush;

        Label l = new Label();
        l.Content = existingProfiles![uiIndex].runnerName;
        l.VerticalAlignment = VerticalAlignment.Center;
        l.Margin = new Thickness(5);

        grid.Child = l;
        grid.PointerPressed += (_, __) => SelectProfile(uiIndex);

        container.Children.Add(grid);
        profileUIS![uiIndex] = grid;
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
        await MainWindow.instance!.DisplayModal<Modal_Settings_Runner>(HandleModal);

        async Task HandleModal(Modal_Settings_Runner modal)
        {
            await modal.HandleOpen(selectedProfile.HasValue ? existingProfiles![selectedProfile.Value].runnerId : null);
        }
    }

    private async Task MakeDefaultProfile()
    {
        if (!selectedProfile.HasValue)
            return;

        //await LibraryHandler.UpdateDefaultWineProfile(existingProfiles![selectedProfile.Value].id);
    }
}
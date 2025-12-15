using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Avalonia.Settings;
using GameLibrary.DB.Database.Tables;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Avalonia.Controls.Settings;

public partial class Control_Settings_Wine_Profiles : UserControl, ISettingControl
{
    private SettingBase? setting;

    public Control_Settings_Wine_Profiles()
    {
        InitializeComponent();
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Wine_Profiles ui)
    {
        this.setting = setting;
        return this;
    }

    public async Task LoadValue()
    {
        dbo_WineProfile[]? profiles = await setting!.LoadSetting<dbo_WineProfile[]>();

        container.Children.Clear();

        if (profiles != null)
        {
            foreach (dbo_WineProfile profile in profiles)
            {
                DrawProfile(profile);
            }
        }
    }

    private void DrawProfile(dbo_WineProfile profile)
    {
        Grid grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));

        grid.Height = 40;

        Label l = new Label();
        l.Content = profile.profileName;

        Common_Button btn = new Common_Button();
        btn.Content = "Edit";

        grid.Children.Add(l);
        grid.Children.Add(btn);

        Grid.SetColumn(l, 0);
        Grid.SetColumn(btn, 1);

        container.Children.Add(grid);
    }
}
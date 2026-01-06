namespace GameLibrary.Logic.Settings.UI;

public struct SettingsUI_Dropdown : ISettingsUI
{
    public string[] options;

    public SettingsUI_Dropdown(string[] options)
    {
        this.options = options;
    }
}

namespace GameLibrary.Logic.Settings.UI;

public class SettingsUI_Title : ISettingsUI
{
    public readonly string title;
    public readonly double margin;

    public SettingsUI_Title(string title, double margin)
    {
        this.title = title;
        this.margin = margin;
    }
}

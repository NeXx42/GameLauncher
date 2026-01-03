namespace GameLibrary.Logic.Settings.UI;

public struct SettingsUI_Toggle : ISettingsUI
{
    public string positiveText;
    public string negativeText;

    public SettingsUI_Toggle(string pos = "Enabled", string neg = "Disabled")
    {
        this.positiveText = pos;
        this.negativeText = neg;
    }
}
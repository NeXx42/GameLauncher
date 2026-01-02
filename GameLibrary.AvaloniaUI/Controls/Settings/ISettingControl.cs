using System.Threading.Tasks;

namespace GameLibrary.AvaloniaUI.Controls.Settings;

public interface ISettingControl
{
    public Task LoadValue();
}

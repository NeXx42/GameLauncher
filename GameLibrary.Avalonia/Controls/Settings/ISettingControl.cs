using System.Threading.Tasks;

namespace GameLibrary.Avalonia.Controls.Settings;

public interface ISettingControl
{
    public Task LoadValue();
}

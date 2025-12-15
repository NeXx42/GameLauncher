using System.Threading.Tasks;

namespace GameLibrary.Avalonia.Settings;

public interface ISettingControl
{
    public Task LoadValue();
}

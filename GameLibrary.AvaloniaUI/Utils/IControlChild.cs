using System.Threading.Tasks;
using GameLibrary.Controller;

namespace GameLibrary.AvaloniaUI.Utils;

public interface IControlChild
{
    public Task Enter();

    public Task<bool> Move(int x, int y); // return whether this has been escaped
    public Task<bool> PressButton(ControllerButton btn);
}

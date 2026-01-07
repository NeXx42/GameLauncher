namespace GameLibrary.Controller;

public enum ControllerButton
{
    A, // bot
    B, // right
    X, // left
    Y, // top


    RightBumper,
    LeftBumper,

    Settings,
}

public interface IControllerInputCallback
{
    public void Move(int x, int y);
    public void PressButton(ControllerButton btn);
}

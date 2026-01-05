using System.Threading;


namespace GameLibrary.AvaloniaUI.Utils;

public static class ControllerInput
{
    //private static ControllerPosition currentPos;

    //public static void Init()
    //{
    //    Thread thread = new Thread(ListenForControllerInput);
    //    thread.Start();
    //}

    //private static void ListenForControllerInput()
    //{
    //    //SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER);
    //    //sdl
    //}
}

public struct ControllerPosition
{
    public int x;
    public int y;
}
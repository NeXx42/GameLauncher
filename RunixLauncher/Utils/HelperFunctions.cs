using System;
using Avalonia.Threading;

namespace GameLibrary.AvaloniaUI.Utils;

public static class HelperFunctions
{
    public static void WrapUIThread(Action evnt)
    {
        Dispatcher.UIThread.Post(evnt);
    }
}

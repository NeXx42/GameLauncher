using System;
using Avalonia.Threading;

namespace GameLibrary.Avalonia.Utils;

public static class HelperFunctions
{
    public static void WrapUIThread(Action evnt)
    {
        Dispatcher.UIThread.Post(evnt);
    }
}

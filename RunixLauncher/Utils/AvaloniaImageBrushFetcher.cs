using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.AvaloniaUI.Utils;

public class AvaloniaImageBrushFetcher : IImageFetcher
{
    public async Task<object?> GetIcon(string absolutePath)
    {
        var bitmap = new Bitmap(absolutePath);
        ImageBrush? brush = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill
            };
        });

        return brush;
    }
}

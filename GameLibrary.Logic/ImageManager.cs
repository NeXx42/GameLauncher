using System.Collections.Concurrent;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Interfaces;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic;

public static class ImageManager
{
    private static IImageFetcher? fetcher;

    private delegate void FetchImageEvent(int id, object? img);


    private static ConcurrentDictionary<int, ImageFetchRequest> queuedImageFetch = new ConcurrentDictionary<int, ImageFetchRequest>();
    private static ConcurrentDictionary<int, object?> cachedImages = new ConcurrentDictionary<int, object?>();

    private static Thread? imageFetchThread;


    public static void Init<T>(T implementation) where T : IImageFetcher
    {
        fetcher = implementation;

        imageFetchThread = new Thread(GameImageFetcher);
        imageFetchThread.Name = "Image Thread";
        imageFetchThread.Start();
    }

    public static async Task GetGameImage<T>(GameDto game, Action<int, T?> onFetch)
    {
        if (cachedImages.TryGetValue(game.gameId, out object? res))
        {
            onFetch?.Invoke(game.gameId, (T?)res);
            return;
        }

        queuedImageFetch.AddOrUpdate(game.gameId, new ImageFetchRequest()
        {
            game = game,
            callback = MediateReturn
        },
        (_, existing) =>
        {
            existing.callback += MediateReturn;
            return existing;
        });

        void MediateReturn(int id, object? img)
        {
            onFetch?.Invoke(id, (T?)img);
        }
    }

    private static async void GameImageFetcher()
    {
        while (true)
        {
            await Task.Delay(10);

            if (queuedImageFetch.Count == 0)
                continue;

            IEnumerable<int> toClear = queuedImageFetch.Keys;

            await Parallel.ForEachAsync(toClear, async (int id, CancellationToken token) =>
            {
                if (!queuedImageFetch.TryGetValue(id, out ImageFetchRequest req))
                    return;

                string? path = await req.game.FetchIconFilePath();

                if (!File.Exists(path))
                {
                    cachedImages.TryAdd(req.game.gameId, null);
                    return;
                }

                object? response = await fetcher!.GetIcon(path);

                DependencyManager.InvokeOnUIThread(() =>
                {
                    req.callback?.Invoke(id, response);
                });

                cachedImages.TryAdd(id, response);
            });

            foreach (int i in toClear)
            {
                if (!queuedImageFetch.TryRemove(i, out _))
                {

                }

            }
        }
    }

    public static void ClearCache(int gameId)
    {
        if (cachedImages.TryRemove(gameId, out _))
        {

        }
    }

    private struct ImageFetchRequest
    {
        public GameDto game;
        public FetchImageEvent callback;
    }
}

using System.Collections.Concurrent;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.Logic;

public static class ImageManager
{
    private static IImageFetcher? fetcher;

    private delegate void FetchImageEvent(int id, object? img);


    private static ConcurrentDictionary<int, ImageFetchRequest> queuedImageFetch = new ConcurrentDictionary<int, ImageFetchRequest>();
    private static ConcurrentDictionary<int, object?> cachedImages = new ConcurrentDictionary<int, object?>();

    private static FetchImageEvent? onGlobalImageChange;
    private static Thread? imageFetchThread;


    public static void Init<T>(T implementation) where T : IImageFetcher
    {
        fetcher = implementation;

        imageFetchThread = new Thread(GameImageFetcher);
        imageFetchThread.Name = "Image Thread";
        imageFetchThread.Start();
    }

    public static void RegisterOnGlobalImageChange<T>(Action<int, T?> callback)
    {
        onGlobalImageChange += (id, img) => callback?.Invoke(id, (T?)img);
    }

    public static async Task GetGameImage<T>(dbo_Game game, Action<int, T?> onFetch)
    {
        if (cachedImages.TryGetValue(game.id, out object? res))
        {
            onFetch?.Invoke(game.id, (T?)res);
            return;
        }

        if (queuedImageFetch.TryGetValue(game.id, out ImageFetchRequest existingFetchRequest))
        {
            existingFetchRequest.callback += MediateReturn;
        }
        else
        {
            string iconPath = await game.GetAbsoluteIconLocation();

            if (!File.Exists(iconPath))
            {
                cachedImages.TryAdd(game.id, null);
                onFetch?.Invoke(game.id, default);

                return;
            }

            queuedImageFetch.TryAdd(game.id, new ImageFetchRequest()
            {
                gameId = game.id,
                absoluteImagePath = await game.GetAbsoluteIconLocation(),
                callback = MediateReturn
            });
        }

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

            List<int> toClear = new List<int>();

            foreach (KeyValuePair<int, ImageFetchRequest> queued in queuedImageFetch)
            {
                object? response = await fetcher!.GetIcon(queued.Value.absoluteImagePath);

                fetcher.InvokeOnUIThread(() =>
                {
                    queued.Value.callback?.Invoke(queued.Value.gameId, response);
                    onGlobalImageChange?.Invoke(queued.Value.gameId, response);
                });

                cachedImages.TryAdd(queued.Key, response);
                toClear.Add(queued.Key);
            }

            foreach (int i in toClear)
                queuedImageFetch.TryRemove(i, out _);
        }
    }

    public static void ClearCache(int gameId)
    {
        if (cachedImages.TryRemove(gameId, out _))
        {
            onGlobalImageChange?.Invoke(gameId, null);
        }
    }

    private struct ImageFetchRequest
    {
        public int gameId;
        public string absoluteImagePath;

        public FetchImageEvent callback;
    }
}

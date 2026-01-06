using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Objects;

public class GameDto_Steam : GameDto
{
    public long appId { private set; get; }

    public GameDto_Steam(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config) : base(game, tags, config)
    {
        appId = long.Parse(game.executablePath!);
    }

    public override string getAbsoluteFolderLocation => folderPath;

    public override async Task<string?> FetchIconFilePath()
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;

        if (iconPath.StartsWith("https://"))
        {
            using (HttpClient client = new HttpClient())
            {
                string folder = getAbsoluteFolderLocation;

                if (!Directory.Exists(folder))
                    return null;

                byte[] bytes = await client.GetByteArrayAsync(iconPath);

                iconPath = Path.Combine(folder, $"{Guid.NewGuid()}.png");
                await File.CreateText(iconPath).DisposeAsync();
                await File.WriteAllBytesAsync(iconPath, bytes);

                await UpdateDatabaseEntry(nameof(dbo_Game.iconPath));
            }
        }

        return iconPath;
    }

    public override async Task Launch()
    {
        await RunnerManager.RunSteamGame(gameId, appId);
    }
}

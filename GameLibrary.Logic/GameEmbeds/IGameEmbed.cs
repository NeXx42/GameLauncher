namespace GameLibrary.Logic.GameEmbeds;

public interface IGameEmbed
{
    /// <summary>
    /// higher = further to the end, so priority infinity, will result in it being the last applied
    /// </summary>
    public int getPriority { get; }
    public void Embed(RunnerManager.GameLaunchData inp, Dictionary<string, string?> args);
}

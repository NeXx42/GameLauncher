using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

public class GameEmbed_Locale : IGameEmbed
{
    public int getPriority => 0;

    public void Embed(RunnerManager.LaunchArguments inp, Dictionary<RunnerDto.RunnerConfigValues, string?> args)
    {
        //inp.environmentArguments.Add("LANG", "ja_JP.UTF-8"); // breaks it?

        inp.environmentArguments.Add("LC_ALL", "ja_JP.UTF-8");
        inp.environmentArguments.Add("LC_CTYPE", "ja_JP.UTF-8");
    }
}

using GameLibrary.Logic.GameRunners;

namespace GameLibrary.Logic.GameEmbeds;

public class GameEmbed_Firejail : IGameEmbed
{
    public int getPriority => 1000;

    public void Embed(RunnerManager.GameLaunchData inp, Dictionary<string, string?> args)
    {
        string actualCommand = inp.command;
        inp.command = "firejail";

        LinkedListNode<string> argumentsEnd = inp.arguments.AddFirst("--noprofile");

        if (args.HasConfigValueOf(RunnerManager.RunnerConfigValues.Generic_Sandbox_BlockNetwork, true))
        {
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--net=none");
            //inp.environmentArguments.Add("WINEDLLOVERRIDES", "wininet=n;dnsapi=n;ws2_32=n");
        }

        if (args.HasConfigValueOf(RunnerManager.RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true))
        {
            //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--read-only=~");

            foreach (string whitelist in inp.whiteListedDirs)
            {
                argumentsEnd = inp.arguments.AddAfter(argumentsEnd, $"--whitelist={whitelist}");
            }

            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--blacklist=~/.ssh");
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--blacklist=~/.gnupg");
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--blacklist=~/.aws");
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--blacklist=~/.config/browser");

            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--dbus-user=none");
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--private-tmp");
        }

        argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--caps.drop=all");
        argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--nodvd");
        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--seccomp"); // breaks slave process

        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--no-ptrace");
        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--device=/dev/dri");

        inp.arguments.AddAfter(argumentsEnd, actualCommand);
    }
}

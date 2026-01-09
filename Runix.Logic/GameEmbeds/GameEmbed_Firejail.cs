using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

public class GameEmbed_Firejail : IGameEmbed
{
    public int getPriority => 1000;

    public void Embed(RunnerManager.LaunchArguments inp, ConfigProvider<RunnerDto.RunnerConfigValues> args)
    {
        string actualCommand = inp.command;
        inp.command = "firejail";

        LinkedListNode<string> argumentsEnd = inp.arguments.AddFirst("--noprofile");

        if (args.GetBoolean(RunnerDto.RunnerConfigValues.Generic_Sandbox_BlockNetwork, true))
        {
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--net=none");
            //inp.environmentArguments.Add("WINEDLLOVERRIDES", "wininet=n;dnsapi=n;ws2_32=n");
        }

        if (args.GetBoolean(RunnerDto.RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true))
        {
            if (args.TryGetValue(RunnerDto.RunnerConfigValues.Wine_SharedDocuments, out string? documentsStorage) && !string.IsNullOrEmpty(documentsStorage))
            {
                argumentsEnd = inp.arguments.AddAfter(argumentsEnd, $"--whitelist={documentsStorage}");
            }

            // hide user folder by default
            argumentsEnd = inp.arguments.AddAfter(argumentsEnd, $"--whitelist={DependencyManager.GetUserStorageFolder()}");
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

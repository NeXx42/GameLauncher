namespace GameLibrary.Logic.ExternalLibraries;

public class ExternalLibrary_Steam : IExternalLibrary
{
    public Task Refresh()
    {
        return Task.CompletedTask;
    }
}

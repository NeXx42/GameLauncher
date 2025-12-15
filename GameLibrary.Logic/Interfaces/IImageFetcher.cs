namespace GameLibrary.Logic.Interfaces;

public interface IImageFetcher
{
    public Task<object?> GetIcon(string absolutePath);

    public void InvokeOnUIThread(Action toRun);
}

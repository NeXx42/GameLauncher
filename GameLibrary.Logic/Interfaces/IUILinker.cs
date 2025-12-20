namespace GameLibrary.Logic.Interfaces;

public interface IUILinker
{
    public Task OpenLoadingModal(bool progressiveLoad, params Func<Task>[] tasks);
    public Task<string?> OpenStringInputModal(string title);
}

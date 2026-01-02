namespace GameLibrary.Logic.Interfaces;

public interface IUILinker
{
    public Task OpenLoadingModal(bool progressiveLoad, params Func<Task>[] tasks);

    public Task<string?> OpenStringInputModal(string title, string? existingText = "");
    public Task<bool> OpenYesNoModal(string title, string paragraph);
    public Task<bool> OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string loadingMessage);
}

namespace GameLibrary.Logic.Interfaces;

public interface IUILinker
{
    public Task OpenLoadingModal(bool progressiveLoad, params Func<Task>[] tasks);

    public Task<string?> OpenStringInputModal(string title, string? existingText = "");
    public Task<bool> OpenYesNoModal(string title, string paragraph);
    public Task<bool> OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string loadingMessage);
    public Task<int> OpenConfirmationAsync(string title, string paragraph, params (string btn, Func<Task> callback, string? loadingMessage)[] controls);
}

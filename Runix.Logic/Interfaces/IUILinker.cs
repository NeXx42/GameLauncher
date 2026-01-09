namespace GameLibrary.Logic.Interfaces;

public interface IUILinker
{
    public void InvokeOnUIThread(Action a);

    public void Quit();

    public Task OpenLoadingModal(bool progressiveLoad, params Func<Task>[] tasks);

    public Task<string?> OpenStringInputModal(string title, string? existingText = "", bool obfuscateInput = false);
    public Task<bool> OpenYesNoModal(string title, string paragraph);
    public Task<bool> OpenYesNoModalAsync(string title, string paragraph, Func<Task> positiveCallback, string loadingMessage);
    public Task<int> OpenConfirmationAsync(string title, string paragraph, params (string btn, Func<Task> callback, string? loadingMessage)[] controls);

    public Task<string[]?> OpenFoldersDialog(string title);
    public Task<string?> OpenFolderDialog(string title);
    public Task<string[]?> OpenFilesDialog(string title, params string[] allowedTypes);
    public Task<string?> OpenFileDialog(string title, params string[] allowedTypes);
}

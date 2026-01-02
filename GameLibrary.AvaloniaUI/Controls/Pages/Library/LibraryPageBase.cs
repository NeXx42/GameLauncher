using System.Threading.Tasks;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public abstract class LibraryPageBase
{
    protected Page_Library library;

    public LibraryPageBase(Page_Library library)
    {
        this.library = library;
    }

    public abstract Task DrawGames();

    public virtual Task FirstPage() { return Task.CompletedTask; }
    public virtual Task NextPage() { return Task.CompletedTask; }
    public virtual Task PrevPage() { return Task.CompletedTask; }
    public virtual Task LastPage() { return Task.CompletedTask; }
}

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class LibraryPage_Endless : LibraryPageBase
{
    public override Panel getGameChild => cont_Games;

    public LibraryPage_Endless(Page_Library library) : base(library)
    {
        InitializeComponent();
    }


    protected override GameFilterRequest GetGameFilter() => library.GetGameFilter(0, 99999);

    public override void ResetLayout() => scroll.Offset = scroll.Offset.WithY(0);
}
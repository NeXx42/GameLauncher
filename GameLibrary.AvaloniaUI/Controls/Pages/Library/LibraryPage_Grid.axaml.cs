using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using GameLibrary.Controller;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class LibraryPage_Grid : LibraryPageBase
{
    public const int ContentPerPage = 18;

    public override Panel getGameChild => cont_Games;
    public override int getNumberOfItemsPerColumn => (int)Math.Max(1, Math.Round(cont_Games.Bounds.Width / (CardSize.width + cont_Games.ItemSpacing)));

    private int page;


    public LibraryPage_Grid(Page_Library library) : base(library)
    {
        InitializeComponent();

        btn_FirstPage.RegisterClick(FirstPage);
        btn_PrevPage.RegisterClick(PrevPage);
        btn_NextPage.RegisterClick(NextPage);
        btn_LastPage.RegisterClick(LastPage);

        for (int i = 0; i < cacheUI.Count; i++)
        {
            CreateGameUI();
        }
    }

    public override Task DrawGames()
    {
        lbl_PageNum.Text = (page + 1).ToString();

        return base.DrawGames();
    }

    public async Task PrevPage() => await UpdatePage(Math.Max(page - 1, 0));
    public async Task NextPage() => await UpdatePage(Math.Min(page + 1, LibraryManager.GetMaxPages(ContentPerPage)));
    public async Task FirstPage() => await UpdatePage(0);
    public async Task LastPage() => await UpdatePage(LibraryManager.GetMaxPages(ContentPerPage));

    private async Task UpdatePage(int to)
    {
        if (page == to)
            return;

        page = to;
        await DrawGames();
    }

    protected override GameFilterRequest GetGameFilter() => library.GetGameFilter(page, ContentPerPage);
    public override void ResetLayout() => page = 0;
}
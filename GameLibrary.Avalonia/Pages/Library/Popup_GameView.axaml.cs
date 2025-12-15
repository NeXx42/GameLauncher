using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_GameView : UserControl
{
    private int inspectingGameId;

    private HashSet<int> gameTags;
    private Dictionary<int, Library_Tag> allTags = new Dictionary<int, Library_Tag>();

    private Page_Library master;

    public Popup_GameView()
    {
        InitializeComponent();

        btn_Delete.RegisterClick(DeleteGame);
        btn_Overlay.RegisterClick(btn_Overlay_Click);

        btn_Browse.RegisterClick(BrowseToGame);
        btn_Launch.RegisterClick(HandleLaunch);

        inp_Emulate.RegisterOnChange(HandleEmulateToggle);
    }

    public void Setup(Page_Library master)
    {
        this.master = master;
        ImageManager.RegisterOnGlobalImageChange<ImageBrush>(UpdateGameIcon);
    }


    public async Task Draw(dbo_Game game)
    {
        inspectingGameId = game.id;

        img_bg.Background = null;
        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);

        await RedrawSelectedTags();

        inp_Emulate.SilentSetValue(game.useEmulator);
        lbl_Title.Content = game.gameName;

        List<string> executableBinaries = await GetBinaries(game);
        inp_binary.Setup(executableBinaries.Select(x => Path.GetFileName(x)), executableBinaries.IndexOf(game.executablePath!), HandleBinaryChange);

        if (ConfigHandler.isOnLinux)
        {
            dbo_WineProfile[] profiles = await DatabaseHandler.GetItems<dbo_WineProfile>();

            inp_WineProfile.IsVisible = true;
            inp_WineProfile.Setup(profiles.Select(x => x.profileName), game.wineProfile, HandleWineProfileChange);
        }
        else
        {
            inp_WineProfile.IsVisible = false;
        }
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGameId != gameId)
            return;

        img_bg.Background = img;
    }

    private async Task<List<string>> GetBinaries(dbo_Game game)
    {
        string gameFolder = await game.GetAbsoluteFolderLocation();

        if (!Directory.Exists(gameFolder))
            return new List<string>();

        return Directory.GetFiles(gameFolder).Where(FilterFile).Select(x => Path.GetFileName(x)).ToList();

        bool FilterFile(string dir)
        {
            return dir.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) ||
                dir.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private async Task RedrawSelectedTags()
    {
        gameTags = (await LibraryHandler.GetGameTags(inspectingGameId)).ToHashSet();

        foreach (KeyValuePair<int, Library_Tag> tag in allTags)
        {
            tag.Value.Margin = new Thickness(0, 0, 0, 5);
            tag.Value.Toggle(gameTags.Contains(tag.Key));
        }
    }

    private void HandleLaunch()
    {
        GameLauncher.LaunchGame(inspectingGameId);
    }

    public void RedrawTags(int[] tags)
    {
        allTags.Clear();
        cont_AllTags.Children.Clear();

        foreach (int tagId in tags)
        {
            GenerateTag(tagId);
        }

        void GenerateTag(int tagId)
        {
            dbo_Tag? tag = LibraryHandler.GetTagById(tagId);

            if (tag != null)
            {
                Library_Tag tagUI = new Library_Tag();
                tagUI.Draw(tag, HandleTagToggle);

                cont_AllTags.Children.Add(tagUI);
                allTags.Add(tagId, tagUI);
            }
        }
    }

    private async void HandleTagToggle(int tagId)
    {
        if (gameTags.Contains(tagId))
        {
            gameTags.Remove(tagId);
            LibraryHandler.RemoveTagFromGame(inspectingGameId, tagId);
        }
        else
        {
            gameTags.Add(tagId);
            LibraryHandler.AddTagToGame(inspectingGameId, tagId);
        }

        await RedrawSelectedTags();
    }

    private void HandleEmulateToggle(bool to)
    {
        LibraryHandler.UpdateGameEmulationStatus(inspectingGameId, to);
    }

    private void HandleWineProfileChange()
    {

    }

    private void btn_Overlay_Click()
    {
        //GameLauncher.RequestOverlay(inspectingGameId, null);
    }

    private async void BrowseToGame() => await FileManager.BrowseToGame(LibraryHandler.GetGameFromId(inspectingGameId)!);

    private async void HandleBinaryChange()
    {
        await LibraryHandler.ChangeBinaryLocation(inspectingGameId, inp_binary.selectedValue?.ToString());
        await Draw(LibraryHandler.GetGameFromId(inspectingGameId)!);
    }

    private async void DeleteGame() => await FileManager.DeleteGame(LibraryHandler.GetGameFromId(inspectingGameId)!);
}
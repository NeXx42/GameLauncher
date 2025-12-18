using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Avalonia.Pages;
using GameLibrary.Avalonia.Utils;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia;

public partial class MainWindow : Window
{
    public static Window? instance { private set; get; }
    private UserControl activePage;

    public MainWindow()
    {
        instance = this;

        InitializeComponent();
        OnStart();
    }

    private async void OnStart()
    {
        await DatabaseManager.Init();

        if (DatabaseManager.cachedDBLocation == null)
        {
            EnterPage<Page_Setup>().Enter(CompleteSetup);
        }
        else
        {
            await DatabaseManager.LoadDatabase(DialogHelper.OpenDatabaseExceptionDialog);
            await HandleAuthentication();
        }
    }

    private async void CompleteSetup(Page_Setup.SetupRequest req)
    {
        await DatabaseManager.CreateDBPointerFile(req.dbFile);
        await DatabaseManager.LoadDatabase(DialogHelper.OpenDatabaseExceptionDialog);

        if (req.isExistingLoad)
        {
            await CompleteLoad();
            return;
        }
        else
        {
            await DatabaseHandler.InsertIntoTable(new dbo_Libraries()
            {
                libaryId = 0,
                rootPath = req.libraryFolder
            });

            if (!string.IsNullOrEmpty(req.pin))
            {
                await ConfigHandler.SaveConfigValue(ConfigHandler.ConfigValues.PasswordHash, req.pin);
            }

            await HandleAuthentication();
        }
    }

    private async Task HandleAuthentication()
    {
        string passwordHash = await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.PasswordHash, string.Empty);

        if (!string.IsNullOrEmpty(passwordHash))
        {
            EnterPage<Page_Login>().Enter(passwordHash, async () => await CompleteLoad());
        }
        else
        {
            await CompleteLoad();
        }
    }

    private async Task CompleteLoad()
    {
        await ConfigHandler.Init();

        OverlayManager.Init(DialogHelper.OpenOverlay);
        ImageManager.Init(new AvaloniaImageBrushFetcher());
        GameLauncher.Init();

        await LibraryHandler.Setup();

        EnterPage<Page_Library>();
    }


    private T EnterPage<T>() where T : UserControl
    {
        if (activePage != null)
        {
            cont_Pages.Children.Remove(activePage);
        }

        T page = Activator.CreateInstance<T>();

        activePage = page;
        cont_Pages.Children.Add(activePage);

        return page;
    }
}
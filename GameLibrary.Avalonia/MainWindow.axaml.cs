using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Avalonia.Pages;
using GameLibrary.Avalonia.Utils;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia;

public partial class MainWindow : Window
{
    public static MainWindow? instance { private set; get; }
    private UserControl activePage;

    public MainWindow()
    {
        instance = this;

        InitializeComponent();
        OnStart();

        cont_Loading.IsVisible = true;
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
        await LoadTasks(false,
            () => DatabaseManager.CreateDBPointerFile(req.dbFile),
            () => DatabaseManager.LoadDatabase(DialogHelper.OpenDatabaseExceptionDialog),
            () => Task.Delay(10000)
        );

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
        await LoadTasks(false, LibraryHandler.Setup, ConfigHandler.Init);

        OverlayManager.Init(DialogHelper.OpenOverlay);
        ImageManager.Init(new AvaloniaImageBrushFetcher());
        GameLauncher.Init();

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
        activePage.ZIndex = 1;

        cont_Pages.Children.Add(activePage);

        return page;
    }

    public static async Task LoadTasks(bool showLoading, params Func<Task>[] tasks)
    {
        instance!.cont_Loading.IsVisible = true;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        if (showLoading)
        {
            instance!.lbl_Loading.Content = "Loading...";

            DateTime lastTime = DateTime.UtcNow;
            double averageTaskLength = 0;
            int tasksCompleted = 0;

            foreach (Func<Task> entry in tasks)
            {
                try
                {
                    await entry();

                    tasksCompleted++;

                    averageTaskLength = averageTaskLength + ((DateTime.UtcNow - lastTime).TotalSeconds - averageTaskLength) / tasksCompleted;
                    lastTime = DateTime.UtcNow;

                    float remainingPercentage = (float)Math.Round((tasksCompleted / tasks.Length) * 100f);
                    float remainingTime = (float)Math.Round(averageTaskLength * (tasks.Length - tasksCompleted));

                    await Dispatcher.UIThread.InvokeAsync(() => instance!.lbl_Loading.Content = $"{remainingPercentage}% {remainingTime}");
                }
                catch (Exception e)
                {
                    await DialogHelper.OpenDatabaseExceptionDialog(e, "");
                }

            }
        }
        else
        {
            instance!.lbl_Loading.Content = "Loading";
            await Task.WhenAll(tasks.Select(x => x()));
        }


        instance!.cont_Loading.IsVisible = false;
    }
}
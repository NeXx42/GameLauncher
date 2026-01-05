using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GameLibrary.AvaloniaUI.Controls.Pages;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.AvaloniaUI.Utils;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI;

public partial class MainWindow : Window
{
    public static MainWindow? instance { private set; get; }
    private UserControl? activePage;

    public MainWindow()
    {
        instance = this;

        InitializeComponent();
        OnStart();

        cont_Modals.IsVisible = false;
        DragDrop.SetAllowDrop(this, true);
    }

    private async void OnStart()
    {
        await DependencyManager.PreSetup(new UILinker(), new AvaloniaImageBrushFetcher());

        if (DependencyManager.cachedDBLocation == null)
        {
            EnterPage<Page_Setup>().Enter(CompleteSetup);
        }
        else
        {
            await DependencyManager.LoadDatabase();
            await HandleAuthentication();
        }
    }

    private async void CompleteSetup(Page_Setup.SetupRequest req)
    {
        await DependencyManager.LoadDatabase(req.dbFile);

        if (req.isExistingLoad)
        {
            await CompleteLoad();
            return;
        }
        else
        {
            // when i add support for multiple libraries this should be done in the setup page
            await LibraryHandler.GenerateLibrary(req.libraryFolder);

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
        await DependencyManager.PostSetup();
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

    public async Task DisplayModal<T>(Func<T, Task> modalReq) where T : UserControl
    {
        cont_Modals.IsVisible = true;
        cont_Pages.Effect = new ImmutableBlurEffect(5);

        T modal = Activator.CreateInstance<T>();
        cont_Modals.Child = modal;

        await modalReq(modal);

        cont_Modals.Child = null;
        cont_Modals.IsVisible = false;
        cont_Pages.Effect = null;
    }

    private void HandleFileDrop()
    {

    }
}
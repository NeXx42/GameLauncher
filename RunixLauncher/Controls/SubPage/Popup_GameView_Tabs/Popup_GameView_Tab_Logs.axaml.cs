using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Logs : Popup_GameView_TabBase
{
    public Popup_GameView_Tab_Logs()
    {
        InitializeComponent();
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        return new Tab_Logs(this, btn);
    }


    private class Tab_Logs : Tab
    {
        private new Popup_GameView_Tab_Logs element;

        public Tab_Logs(Popup_GameView_Tab_Logs element, Common_ButtonToggle btn) : base(element, btn)
        {
            this.element = element;
        }

        protected override void InternalSetup(TabGroup master) { }

        protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
        {
            await RefreshLogs(game);
        }

        private async Task RefreshLogs(GameDto? game)
        {
            if (game == null)
            {
                element.lbl_Logs.Text = "";
                return;
            }

            element.lbl_Logs.Text = await game.ReadLogs();
        }
    }
}
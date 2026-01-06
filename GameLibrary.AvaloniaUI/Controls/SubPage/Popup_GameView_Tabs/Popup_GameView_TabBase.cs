using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.AvaloniaUI.Controls.SubPage;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.SubPages.Popup_GameView_Tabs;



public abstract class Popup_GameView_TabBase : UserControl
{
    public class TabGroup : UITabGroup
    {
        public Popup_GameView master;

        public TabGroup(Popup_GameView master, params Tab[] tabs) : base()
        {
            this.groups = tabs;
            this.master = master;

            for (int i = 0; i < this.groups.Length; i++)
                this.groups[i].Setup(this, i);
        }

        public async Task OpenFresh()
        {
            int temp = selectedGroup ?? 0;
            selectedGroup = null;

            await ChangeSelection(temp);
        }

        public override async Task ChangeSelection(int to)
        {
            await base.ChangeSelection(to);
        }
    }

    public abstract class Tab : UITabGroup_Group
    {
        protected TabGroup? master;
        protected Common_ButtonToggle toggleBtn;

        protected int? lastGameId;
        protected GameDto inspectingGame => master!.master.inspectingGame!;

        public Tab(Control element, Common_ButtonToggle btn) : base(element, btn)
        {
            toggleBtn = btn;
            btn.autoToggle = false;
        }

        public sealed override void Setup(UITabGroup master, int index)
        {
            this.master = (TabGroup)master;

            toggleBtn.Register(async (_) => await master.ChangeSelection(index));
            element.IsVisible = false;

            InternalSetup(this.master);
        }

        public sealed override Task Close()
        {
            toggleBtn.isSelected = false;
            return base.Close();
        }

        public sealed override async Task Open()
        {
            toggleBtn.isSelected = true;

            await base.Open();
            await OpenWithGame(inspectingGame, inspectingGame.gameId != lastGameId);
            lastGameId = inspectingGame?.gameId;
        }

        protected abstract void InternalSetup(TabGroup master);
        protected abstract Task OpenWithGame(GameDto? game, bool isNewGame);
    }

    public abstract Tab CreateGroup(Common_ButtonToggle btn, Panel parent);
}

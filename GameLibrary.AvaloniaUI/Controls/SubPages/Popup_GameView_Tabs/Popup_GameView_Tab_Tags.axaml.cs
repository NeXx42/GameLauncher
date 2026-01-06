using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Tags : Popup_GameView_TabBase
{
    public Popup_GameView_Tab_Tags()
    {
        InitializeComponent();
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        return new Tab_Tags(this, btn);
    }

    internal class Tab_Tags : Tab
    {
        private new Popup_GameView_Tab_Tags element;
        private Dictionary<int, Library_Tag> allTags = new Dictionary<int, Library_Tag>();

        public Tab_Tags(Popup_GameView_Tab_Tags element, Common_ButtonToggle btn) : base(element, btn)
        {
            this.element = element;
        }

        protected override void InternalSetup(TabGroup master) { }

        protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
        {
            if (isNewGame)
            {
                await CheckForNewTags();
                await RedrawSelectedTags(game!);
            }
        }

        public async Task CheckForNewTags()
        {
            TagDto[] newTags = await TagManager.GetAllTags();

            if (allTags.Count == newTags.Length)
                return;

            allTags.Clear();
            element.cont_AllTags.Children.Clear();

            foreach (TagDto tag in newTags)
            {
                GenerateTag(tag);
            }

            void GenerateTag(TagDto tag)
            {
                Library_Tag tagUI = new Library_Tag();

                if (tag is TagDto_Managed)
                {
                    tagUI.Draw(tag, null);
                    tagUI.Toggle(true);
                }
                else
                {
                    tagUI.Draw(tag, HandleTagToggle);
                }

                element.cont_AllTags.Children.Add(tagUI);
                allTags.Add(tag.id, tagUI);
            }
        }

        private async void HandleTagToggle(TagDto tag)
        {
            if (tag is TagDto_Managed)
                return;

            await inspectingGame!.ToggleTag(tag.id);
            await RedrawSelectedTags(inspectingGame);
        }

        private async Task RedrawSelectedTags(GameDto game)
        {
            foreach (KeyValuePair<int, Library_Tag> tag in allTags)
            {
                if (tag.Key < 0)
                    continue;

                tag.Value.Margin = new Thickness(0, 0, 0, 5);
                tag.Value.Toggle(game?.tags.Contains(tag.Key) ?? false);
            }
        }
    }
}
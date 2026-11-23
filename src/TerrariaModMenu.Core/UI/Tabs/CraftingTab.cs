using System.Drawing;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class CraftingTab : Panel
    {
        private readonly CheckBox _chkNoStations;
        private readonly CheckBox _chkFreeCraft;
        private readonly CheckBox _chkUnlockAll;

        public CraftingTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpRules = DarkTheme.CreateGroup("Crafting Overrides", 8, 8, 538, 178);

            _chkNoStations = DarkTheme.CreateCheck(
                "Ignore Crafting Stations (No Tables / Liquids / Biomes)",
                14, 28, cfg.CraftIgnoreStations,
                v => { cfg.CraftIgnoreStations = v; GameActions.EnqueueRefreshRecipes(); });

            var lblDesc1 = new Label
            {
                Text = "Craft recipes using your inventory materials without needing nearby stations.",
                Location = new Point(32, 50),
                Size = new Size(490, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal
            };

            _chkFreeCraft = DarkTheme.CreateCheck(
                "Free Crafting (Do Not Consume Ingredients)",
                14, 76, cfg.CraftFreeNoConsume,
                v => { cfg.CraftFreeNoConsume = v; GameActions.EnqueueRefreshRecipes(); });

            var lblDesc2 = new Label
            {
                Text = "Crafts items without deducting ingredients from inventory or chests.",
                Location = new Point(32, 98),
                Size = new Size(490, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal
            };

            _chkUnlockAll = DarkTheme.CreateCheck(
                "Unlock All Recipes (Craft Anything Free)",
                14, 124, cfg.CraftUnlockAllRecipes,
                v => { cfg.CraftUnlockAllRecipes = v; GameActions.EnqueueRefreshRecipes(); });
            _chkUnlockAll.ForeColor = DarkTheme.AccentAmber;
            _chkUnlockAll.Font = DarkTheme.FontBold;

            var lblDesc3 = new Label
            {
                Text = "Exposes every recipe in the crafting list with no station or material requirements.",
                Location = new Point(32, 148),
                Size = new Size(490, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal
            };

            grpRules.Controls.AddRange(new Control[]
            {
                _chkNoStations, lblDesc1, _chkFreeCraft, lblDesc2, _chkUnlockAll, lblDesc3
            });
            Controls.Add(grpRules);

            var grpPresets = DarkTheme.CreateGroup("Presets", 8, 196, 538, 118);

            var btnVanilla = DarkTheme.CreateButton("Vanilla", 14, 28, 162, 36, DarkTheme.BgInput, () =>
            {
                _chkNoStations.Checked = false;
                _chkFreeCraft.Checked = false;
                _chkUnlockAll.Checked = false;
                GameActions.EnqueueRefreshRecipes();
            });

            var btnNoTable = DarkTheme.CreateButton("No Stations Only", 186, 28, 168, 36, DarkTheme.AccentBlue, () =>
            {
                _chkNoStations.Checked = true;
                _chkFreeCraft.Checked = false;
                _chkUnlockAll.Checked = false;
                GameActions.EnqueueRefreshRecipes();
            });

            var btnGodCraft = DarkTheme.CreateButton("Unlock All + Free", 364, 28, 162, 36, DarkTheme.AccentPurple, () =>
            {
                _chkNoStations.Checked = true;
                _chkFreeCraft.Checked = true;
                _chkUnlockAll.Checked = true;
                GameActions.EnqueueRefreshRecipes();
            });

            var btnRefresh = DarkTheme.CreateButton("Refresh Recipe List", 14, 74, 512, 30, DarkTheme.AccentGreen, GameActions.EnqueueRefreshRecipes);
            grpPresets.Controls.AddRange(new Control[] { btnVanilla, btnNoTable, btnGodCraft, btnRefresh });
            Controls.Add(grpPresets);
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkNoStations.Checked = cfg.CraftIgnoreStations;
            _chkFreeCraft.Checked = cfg.CraftFreeNoConsume;
            _chkUnlockAll.Checked = cfg.CraftUnlockAllRecipes;
        }
    }
}

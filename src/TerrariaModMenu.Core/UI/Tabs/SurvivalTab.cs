using System.Drawing;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class SurvivalTab : Panel
    {
        private readonly CheckBox _chkGod;
        private readonly CheckBox _chkMana;
        private readonly CheckBox _chkNoFall;
        private readonly CheckBox _chkFlight;
        private readonly CheckBox _chkBreath;
        private readonly CheckBox _chkNoKb;
        private readonly CheckBox _chkNoClip;

        private readonly TrackBar _barNoClipSpd;
        private readonly TrackBar _barMoveSpd;
        private readonly TrackBar _barJump;
        private readonly TrackBar _barDefense;
        private readonly TrackBar _barRegen;
        private readonly TrackBar _barReach;
        private readonly TrackBar _barMining;
        private readonly TrackBar _barDamage;
        private readonly TrackBar _barCrit;

        public SurvivalTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;
            var tip = new ToolTip();

            var grpCore = DarkTheme.CreateGroup("Survival", 8, 8, 258, 256);
            _chkGod = DarkTheme.CreateCheck("God Mode (Invincible + Full HP)", 12, 24, cfg.GodMode, v => cfg.GodMode = v,
                "Enables native God Mode and keeps health at maximum.", tip);
            _chkMana = DarkTheme.CreateCheck("Infinite Mana (Zero Cost)", 12, 50, cfg.InfiniteMana, v => cfg.InfiniteMana = v);
            _chkNoFall = DarkTheme.CreateCheck("No Fall Damage", 12, 76, cfg.NoFallDamage, v => cfg.NoFallDamage = v);
            _chkFlight = DarkTheme.CreateCheck("Infinite Flight / Wings & Boots", 12, 102, cfg.InfiniteFlight, v => cfg.InfiniteFlight = v);
            _chkBreath = DarkTheme.CreateCheck("Infinite Breath + Lava/Water Walk", 12, 128, cfg.InfiniteBreathAndLava, v => cfg.InfiniteBreathAndLava = v);
            _chkNoKb = DarkTheme.CreateCheck("No Knockback", 12, 154, cfg.NoKnockback, v => cfg.NoKnockback = v);
            _chkNoClip = DarkTheme.CreateCheck("NoClip / Free Fly (WASD)", 12, 180, cfg.NoClip, v => cfg.NoClip = v);

            (_barNoClipSpd, _) = DarkTheme.CreateSliderRow(
                grpCore, "NoClip Speed", 12, 206, 232, 4, 40, (int)cfg.NoClipSpeed,
                v => $"{v} px/t", v => cfg.NoClipSpeed = v);

            grpCore.Controls.AddRange(new Control[] { _chkGod, _chkMana, _chkNoFall, _chkFlight, _chkBreath, _chkNoKb, _chkNoClip });
            Controls.Add(grpCore);

            var grpStats = DarkTheme.CreateGroup("Player Stat Modifiers", 274, 8, 272, 412);
            (_barMoveSpd, _) = DarkTheme.CreateSliderRow(grpStats, "Move Speed", 12, 24, 248, 10, 50, 10,
                v => $"{v / 10f:0.0}x", v => cfg.MoveSpeedMultiplier = v / 10f);
            (_barJump, _) = DarkTheme.CreateSliderRow(grpStats, "Jump Boost", 12, 72, 248, 0, 150, 0,
                v => $"+{v / 10f:0.0}", v => cfg.JumpBoostBonus = v / 10f);
            (_barDefense, _) = DarkTheme.CreateSliderRow(grpStats, "Bonus Defense", 12, 120, 248, 0, 999, 0,
                v => $"+{v}", v => cfg.BonusDefense = v);
            (_barRegen, _) = DarkTheme.CreateSliderRow(grpStats, "Bonus Life Regen", 12, 168, 248, 0, 500, 0,
                v => $"+{v}", v => cfg.BonusLifeRegen = v);
            (_barReach, _) = DarkTheme.CreateSliderRow(grpStats, "Tile / Block Reach", 12, 216, 248, 0, 60, 0,
                v => $"+{v} tiles", v => cfg.ReachBonusTiles = v);
            (_barMining, _) = DarkTheme.CreateSliderRow(grpStats, "Mining & Build Speed", 12, 264, 248, 10, 100, 10,
                v => $"{v / 10f:0.0}x", v => cfg.MiningSpeedMultiplier = v / 10f);
            (_barDamage, _) = DarkTheme.CreateSliderRow(grpStats, "Global Damage", 12, 312, 248, 10, 250, 10,
                v => $"{v / 10f:0.0}x", v => cfg.GlobalDamageMultiplier = v / 10f);
            (_barCrit, _) = DarkTheme.CreateSliderRow(grpStats, "Bonus Crit Chance", 12, 360, 248, 0, 100, 0,
                v => $"+{v}%", v => cfg.BonusCritChance = v);
            Controls.Add(grpStats);

            var grpActions = DarkTheme.CreateGroup("Quick Actions", 8, 272, 258, 148);
            var btnHeal = DarkTheme.CreateButton("Full Heal + Mana", 12, 26, 234, 32, DarkTheme.AccentGreen, GameActions.EnqueueFullHeal);
            var btnMap = DarkTheme.CreateButton("Reveal Full World Map", 12, 66, 234, 32, DarkTheme.AccentAmber, GameActions.EnqueueRevealFullMap);
            btnMap.ForeColor = Color.Black;
            var btnResetStats = DarkTheme.CreateButton("Reset Stat Sliders", 12, 106, 234, 30, DarkTheme.BgInput, ResetSliders);
            grpActions.Controls.AddRange(new Control[] { btnHeal, btnMap, btnResetStats });
            Controls.Add(grpActions);
        }

        private void ResetSliders()
        {
            _barMoveSpd.Value = 10;
            _barJump.Value = 0;
            _barDefense.Value = 0;
            _barRegen.Value = 0;
            _barReach.Value = 0;
            _barMining.Value = 10;
            _barDamage.Value = 10;
            _barCrit.Value = 0;
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkGod.Checked = cfg.GodMode;
            _chkMana.Checked = cfg.InfiniteMana;
            _chkNoFall.Checked = cfg.NoFallDamage;
            _chkFlight.Checked = cfg.InfiniteFlight;
            _chkBreath.Checked = cfg.InfiniteBreathAndLava;
            _chkNoKb.Checked = cfg.NoKnockback;
            _chkNoClip.Checked = cfg.NoClip;
            _barNoClipSpd.Value = (int)cfg.NoClipSpeed;
            ResetSliders();
        }
    }
}

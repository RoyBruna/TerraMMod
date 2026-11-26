using System;
using System.Drawing;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class SummonerTab : Panel
    {
        private readonly Label _lblCountInfo;
        private readonly Label _lblWhipInfo;

        private readonly TrackBar _barMaxMinions;
        private readonly TrackBar _barMaxSentries;
        private readonly TrackBar _barSummonDmg;
        private readonly TrackBar _barMinionKb;

        private readonly TrackBar _barWhipRange;
        private readonly TrackBar _barWhipSpeed;
        private readonly TrackBar _barMaxTags;
        private readonly TrackBar _barTagDuration;

        public SummonerTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpTelemetry = DarkTheme.CreateGroup("Live Summoner Telemetry", 8, 8, 538, 66);

            _lblCountInfo = new Label
            {
                Text = "Minions: 0 active (0.0 / - slots) | Sentries: 0 / -",
                Location = new Point(12, 22),
                Size = new Size(514, 18),
                ForeColor = DarkTheme.AccentPurple,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };

            _lblWhipInfo = new Label
            {
                Text = "Summon DMG: - | Whip Range: - | Whip Speed: -",
                Location = new Point(12, 42),
                Size = new Size(514, 18),
                ForeColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };

            grpTelemetry.Controls.AddRange(new Control[] { _lblCountInfo, _lblWhipInfo });
            Controls.Add(grpTelemetry);

            var grpArmy = DarkTheme.CreateGroup("Minion & Sentry Army Limits", 8, 80, 538, 152);

            (_barMaxMinions, _) = DarkTheme.CreateSliderRow(grpArmy, "Bonus Max Minions", 12, 24, 244, 0, 100, cfg.BonusMaxMinions,
                v => $"+{v}", v => cfg.BonusMaxMinions = v);

            (_barMaxSentries, _) = DarkTheme.CreateSliderRow(grpArmy, "Bonus Max Sentries / Turrets", 12, 68, 244, 0, 50, cfg.BonusMaxSentries,
                v => $"+{v}", v => cfg.BonusMaxSentries = v);

            (_barSummonDmg, _) = DarkTheme.CreateSliderRow(grpArmy, "Summon Damage Multiplier", 280, 24, 244, 10, 100, (int)(cfg.SummonDamageMultiplier * 10f),
                v => $"{v / 10f:0.0}x", v => cfg.SummonDamageMultiplier = v / 10f);

            (_barMinionKb, _) = DarkTheme.CreateSliderRow(grpArmy, "Bonus Minion Knockback", 280, 68, 244, 0, 150, (int)(cfg.BonusMinionKnockback * 10f),
                v => $"+{v / 10f:0.0}", v => cfg.BonusMinionKnockback = v / 10f);

            var btnPreset10 = DarkTheme.CreateButton("Squad (+10 / +5)", 12, 114, 166, 28, DarkTheme.AccentBlue, () => ApplyArmyPreset(10, 5));
            var btnPreset50 = DarkTheme.CreateButton("Legion (+50 / +20)", 186, 114, 166, 28, DarkTheme.AccentPurple, () => ApplyArmyPreset(50, 20));
            var btnPreset100 = DarkTheme.CreateButton("Max Army (+100 / +50)", 360, 114, 166, 28, DarkTheme.AccentAmber, () => ApplyArmyPreset(100, 50));
            btnPreset100.ForeColor = Color.Black;

            grpArmy.Controls.AddRange(new Control[] { btnPreset10, btnPreset50, btnPreset100 });
            Controls.Add(grpArmy);

            var grpWhips = DarkTheme.CreateGroup("Whip Speed, Range & Tag Stacking", 8, 238, 538, 120);

            (_barWhipRange, _) = DarkTheme.CreateSliderRow(grpWhips, "Whip Range Multiplier", 12, 24, 244, 10, 50, (int)(cfg.WhipRangeMultiplier * 10f),
                v => $"{v / 10f:0.0}x", v => cfg.WhipRangeMultiplier = v / 10f);

            (_barWhipSpeed, _) = DarkTheme.CreateSliderRow(grpWhips, "Whip Attack Speed", 12, 68, 244, 10, 50, (int)(cfg.WhipSpeedMultiplier * 10f),
                v => $"{v / 10f:0.0}x", v => cfg.WhipSpeedMultiplier = v / 10f);

            (_barMaxTags, _) = DarkTheme.CreateSliderRow(grpWhips, "Bonus Whip Tag Stacks", 280, 24, 244, 0, 10, cfg.BonusMaxWhipTags,
                v => $"+{v}", v => cfg.BonusMaxWhipTags = v);

            (_barTagDuration, _) = DarkTheme.CreateSliderRow(grpWhips, "Bonus Tag Duration", 280, 68, 244, 0, 30, (int)cfg.BonusWhipTagDuration,
                v => $"+{v}s", v => cfg.BonusWhipTagDuration = v);

            Controls.Add(grpWhips);

            var grpActions = DarkTheme.CreateGroup("Quick Summoner Actions", 8, 364, 538, 94);

            var btnBuffs = DarkTheme.CreateButton("Summoner Buffs (30m)", 12, 22, 166, 28, DarkTheme.AccentPurple, GameActions.EnqueueApplySummonerBuffs);
            var btnClearMinions = DarkTheme.CreateButton("Despawn Minions", 186, 22, 166, 28, DarkTheme.AccentRed, GameActions.EnqueueDespawnAllMinions);
            var btnClearSentries = DarkTheme.CreateButton("Despawn Sentries", 360, 22, 166, 28, DarkTheme.AccentRed, GameActions.EnqueueDespawnAllSentries);
            var btnReset = DarkTheme.CreateButton("Reset Summoner Defaults", 12, 56, 514, 28, DarkTheme.BgInput, () =>
            {
                ModConfig.Instance.ResetSummonerOnly();
                SyncFromConfig();
            });

            grpActions.Controls.AddRange(new Control[] { btnBuffs, btnClearMinions, btnClearSentries, btnReset });
            Controls.Add(grpActions);
        }

        private void ApplyArmyPreset(int minions, int sentries)
        {
            _barMaxMinions.Value = Math.Max(_barMaxMinions.Minimum, Math.Min(_barMaxMinions.Maximum, minions));
            _barMaxSentries.Value = Math.Max(_barMaxSentries.Minimum, Math.Min(_barMaxSentries.Maximum, sentries));
        }

        public void RefreshTelemetry()
        {
            try
            {
                SummonerTelemetry t = GameActions.CaptureSummonerTelemetry();
                if (!t.InWorld)
                {
                    _lblCountInfo.Text = "Minions: 0 active (0.0 / - slots) | Sentries: 0 / -";
                    _lblWhipInfo.Text = "Summon DMG: - | Whip Range: - | Whip Speed: -";
                    return;
                }

                float whipSpd = t.WhipUseTimeMult > 0.01f ? (1f / t.WhipUseTimeMult) : 1f;
                _lblCountInfo.Text = $"Minions: {t.ActiveMinions} active ({t.UsedMinionSlots:0.0} / {t.MaxMinions} slots) | Sentries: {t.ActiveSentries} / {t.MaxSentries}";
                _lblWhipInfo.Text = $"Summon DMG: {t.MinionDamageMult:0.00}x | Whip Range: {t.WhipRangeMult:0.00}x | Whip Speed: {whipSpd:0.00}x";
            }
            catch (Exception ex)
            {
                ModLogger.Error("SummonerTab.RefreshTelemetry", ex);
            }
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _barMaxMinions.Value = Clamp(_barMaxMinions, cfg.BonusMaxMinions);
            _barMaxSentries.Value = Clamp(_barMaxSentries, cfg.BonusMaxSentries);
            _barSummonDmg.Value = Clamp(_barSummonDmg, (int)(cfg.SummonDamageMultiplier * 10f));
            _barMinionKb.Value = Clamp(_barMinionKb, (int)(cfg.BonusMinionKnockback * 10f));
            _barWhipRange.Value = Clamp(_barWhipRange, (int)(cfg.WhipRangeMultiplier * 10f));
            _barWhipSpeed.Value = Clamp(_barWhipSpeed, (int)(cfg.WhipSpeedMultiplier * 10f));
            _barMaxTags.Value = Clamp(_barMaxTags, cfg.BonusMaxWhipTags);
            _barTagDuration.Value = Clamp(_barTagDuration, (int)cfg.BonusWhipTagDuration);
        }

        private static int Clamp(TrackBar bar, int value) => Math.Max(bar.Minimum, Math.Min(bar.Maximum, value));
    }
}

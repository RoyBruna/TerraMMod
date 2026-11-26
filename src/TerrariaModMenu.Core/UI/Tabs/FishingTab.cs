using System;
using System.Drawing;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class FishingTab : Panel
    {
        private readonly Label _lblPowerInfo;
        private readonly Label _lblQuestInfo;

        private readonly CheckBox _chkMultiBobber;
        private readonly TrackBar _barBobberCount;
        private readonly TrackBar _barSpread;
        private readonly CheckBox _chkInstantBite;
        private readonly CheckBox _chkAutoReel;
        private readonly CheckBox _chkAutoRecast;
        private readonly ComboBox _cmbReelFilter;
        private readonly CheckBox _chkNeverBreak;
        private readonly CheckBox _chkInfBait;
        private readonly CheckBox _chkVirtualBait;

        private readonly TrackBar _barPowerBonus;
        private readonly TrackBar _barCrateChance;
        private readonly CheckBox _chkGuaranteedCrate;
        private readonly CheckBox _chkBoostRare;
        private readonly CheckBox _chkIgnorePond;
        private readonly CheckBox _chkLavaFish;
        private readonly CheckBox _chkSonarCrate;
        private readonly CheckBox _chkMultiQuest;

        public FishingTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpTelemetry = DarkTheme.CreateGroup("Live Fishing Telemetry", 8, 8, 538, 66);

            _lblPowerInfo = new Label
            {
                Text = "Power: - | Pole: - | Bait: - | Bobbers: 0 active",
                Location = new Point(12, 22),
                Size = new Size(514, 18),
                ForeColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };

            _lblQuestInfo = new Label
            {
                Text = "Angler Quest: - | Completed: 0",
                Location = new Point(12, 42),
                Size = new Size(514, 18),
                ForeColor = DarkTheme.AccentAmber,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };

            grpTelemetry.Controls.AddRange(new Control[] { _lblPowerInfo, _lblQuestInfo });
            Controls.Add(grpTelemetry);

            var grpAuto = DarkTheme.CreateGroup("Multi-Bobber & Auto-Fishing", 8, 80, 538, 210);

            _chkMultiBobber = DarkTheme.CreateCheck("Multi-Bobber Casting", 12, 24, cfg.FishingMultiBobberEnabled, v => cfg.FishingMultiBobberEnabled = v);
            _chkMultiBobber.ForeColor = DarkTheme.AccentAmber;
            _chkMultiBobber.Font = DarkTheme.FontBold;

            (_barBobberCount, _) = DarkTheme.CreateSliderRow(grpAuto, "Lines per Cast", 12, 48, 244, 2, 50, cfg.FishingBobberCount,
                v => $"{v}x", v => cfg.FishingBobberCount = v);

            (_barSpread, _) = DarkTheme.CreateSliderRow(grpAuto, "Cast Fan Spread", 12, 92, 244, 5, 100, cfg.FishingCastSpreadPercent,
                v => $"{v}%", v => cfg.FishingCastSpreadPercent = v);

            _chkInstantBite = DarkTheme.CreateCheck("Instant Bite (Zero Wait Timer)", 12, 140, cfg.FishingInstantBite, v => cfg.FishingInstantBite = v);
            _chkInstantBite.ForeColor = DarkTheme.AccentCyan;
            _chkInstantBite.Font = DarkTheme.FontBold;

            _chkAutoReel = DarkTheme.CreateCheck("Auto-Reel on Bite", 12, 164, cfg.FishingAutoReel, v => cfg.FishingAutoReel = v);
            _chkAutoReel.ForeColor = DarkTheme.AccentGreen;
            _chkAutoReel.Font = DarkTheme.FontBold;

            _chkAutoRecast = DarkTheme.CreateCheck("Auto-Recast (AFK Fishing Loop)", 12, 186, cfg.FishingAutoRecast, v => cfg.FishingAutoRecast = v);
            _chkAutoRecast.ForeColor = DarkTheme.AccentGreen;

            var lblFilter = new Label
            {
                Text = "Auto-Reel Catch Filter:",
                Location = new Point(280, 24),
                Size = new Size(244, 16),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal,
                UseMnemonic = false
            };

            _cmbReelFilter = new ComboBox
            {
                Location = new Point(280, 42),
                Size = new Size(244, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontNormal
            };
            _cmbReelFilter.Items.AddRange(new object[]
            {
                "All Catches (Items + Enemies)",
                "Items Only (Skip Enemies)",
                "Crates Only",
                "Crates & Quest Fish"
            });
            _cmbReelFilter.SelectedIndex = Math.Max(0, Math.Min(3, cfg.FishingAutoReelFilter));
            _cmbReelFilter.SelectedIndexChanged += (_, _) =>
            {
                if (_cmbReelFilter.SelectedIndex >= 0)
                {
                    cfg.FishingAutoReelFilter = _cmbReelFilter.SelectedIndex;
                }
            };

            _chkNeverBreak = DarkTheme.CreateCheck("Never Break Fishing Line", 280, 74, cfg.FishingNeverBreakLine, v => cfg.FishingNeverBreakLine = v);
            _chkInfBait = DarkTheme.CreateCheck("Infinite Bait (Never Consume)", 280, 98, cfg.FishingInfiniteBait, v => cfg.FishingInfiniteBait = v);
            _chkVirtualBait = DarkTheme.CreateCheck("Virtual Master Bait (Fish w/o Bait)", 280, 122, cfg.FishingVirtualBait, v => cfg.FishingVirtualBait = v);

            var btnReelAll = DarkTheme.CreateButton("Reel All Bobbers", 280, 158, 126, 32, DarkTheme.AccentCyan, GameActions.EnqueueReelAllBobbers);
            btnReelAll.ForeColor = Color.Black;
            var btnResetFish = DarkTheme.CreateButton("Reset Fishing", 412, 158, 112, 32, DarkTheme.BgInput, () =>
            {
                ModConfig.Instance.ResetFishingOnly();
                SyncFromConfig();
                GameActions.EnqueueResetFishingState();
            });

            grpAuto.Controls.AddRange(new Control[]
            {
                _chkMultiBobber, _chkInstantBite, _chkAutoReel, _chkAutoRecast,
                lblFilter, _cmbReelFilter, _chkNeverBreak, _chkInfBait, _chkVirtualBait, btnReelAll, btnResetFish
            });
            Controls.Add(grpAuto);

            var grpLoot = DarkTheme.CreateGroup("Fishing Power, Pool & Loot Boosters", 8, 296, 538, 168);

            (_barPowerBonus, _) = DarkTheme.CreateSliderRow(grpLoot, "Bonus Fishing Power", 12, 24, 244, 0, 500, cfg.FishingPowerBonus,
                v => $"+{v}%", v => cfg.FishingPowerBonus = v);

            (_barCrateChance, _) = DarkTheme.CreateSliderRow(grpLoot, "Extra Crate Chance", 12, 70, 244, 0, 100, cfg.FishingCrateChance,
                v => $"+{v}%", v => cfg.FishingCrateChance = v);

            _chkGuaranteedCrate = DarkTheme.CreateCheck("Guaranteed Crate Every Catch", 12, 120, cfg.FishingGuaranteedCrate, v => cfg.FishingGuaranteedCrate = v);
            _chkGuaranteedCrate.ForeColor = DarkTheme.AccentAmber;
            _chkGuaranteedCrate.Font = DarkTheme.FontBold;

            _chkBoostRare = DarkTheme.CreateCheck("Force Rare / VeryRare / Legendary", 280, 24, cfg.FishingBoostRare, v => cfg.FishingBoostRare = v);
            _chkBoostRare.ForeColor = DarkTheme.AccentPurple;
            _chkBoostRare.Font = DarkTheme.FontBold;

            _chkIgnorePond = DarkTheme.CreateCheck("Ignore Tiny Pond Penalty (300+)", 280, 50, cfg.FishingIgnorePondSize, v => cfg.FishingIgnorePondSize = v);
            _chkLavaFish = DarkTheme.CreateCheck("Allow Lava Fishing (Any Rod)", 280, 76, cfg.FishingLavaEnabled, v => cfg.FishingLavaEnabled = v);
            _chkSonarCrate = DarkTheme.CreateCheck("Permanent Sonar & Crate Effects", 280, 102, cfg.FishingSonarAndCrate, v => cfg.FishingSonarAndCrate = v);
            _chkMultiQuest = DarkTheme.CreateCheck("Stackable Quest Fish (Always Drop)", 280, 128, cfg.FishingAllowMultipleQuestFish, v => cfg.FishingAllowMultipleQuestFish = v);

            grpLoot.Controls.AddRange(new Control[]
            {
                _chkGuaranteedCrate, _chkBoostRare, _chkIgnorePond, _chkLavaFish, _chkSonarCrate, _chkMultiQuest
            });
            Controls.Add(grpLoot);

            var grpAngler = DarkTheme.CreateGroup("Angler Quests & Fishing Kits", 8, 470, 538, 96);

            var btnNextQuest = DarkTheme.CreateButton("Next / Reroll Quest", 12, 22, 166, 28, DarkTheme.AccentBlue, GameActions.EnqueueRerollAnglerQuest);
            var btnResetDaily = DarkTheme.CreateButton("Reset Daily Limit", 186, 22, 166, 28, DarkTheme.AccentCyan, GameActions.EnqueueResetAnglerDaily);
            btnResetDaily.ForeColor = Color.Black;
            var btnSpawnFish = DarkTheme.CreateButton("Spawn Quest Fish", 360, 22, 166, 28, DarkTheme.AccentAmber, GameActions.EnqueueSpawnCurrentQuestFish);
            btnSpawnFish.ForeColor = Color.Black;

            var btnBuffs = DarkTheme.CreateButton("Fishing Buffs (30m)", 12, 56, 166, 28, DarkTheme.AccentPurple, GameActions.EnqueueApplyFishingBuffs);
            var btnMasterKit = DarkTheme.CreateButton("Master Fishing Kit", 186, 56, 166, 28, DarkTheme.AccentGreen, GameActions.EnqueueGiveMasterFishingKit);
            var btnAnglerSet = DarkTheme.CreateButton("Angler Armor Set", 360, 56, 166, 28, DarkTheme.BgInput, GameActions.EnqueueGiveAnglerArmorSet);

            grpAngler.Controls.AddRange(new Control[]
            {
                btnNextQuest, btnResetDaily, btnSpawnFish,
                btnBuffs, btnMasterKit, btnAnglerSet
            });
            Controls.Add(grpAngler);
        }

        public void RefreshTelemetry()
        {
            try
            {
                FishingTelemetry t = FishingPatches.CaptureTelemetry();
                if (!t.InWorld)
                {
                    _lblPowerInfo.Text = "Power: - | Pole: - | Bait: - | Bobbers: 0 active";
                    _lblQuestInfo.Text = "Angler Quest: Join a world to view live quest fish";
                    return;
                }

                string bitePart = t.BitingBobbers > 0 ? $" ({t.BitingBobbers} biting)" : string.Empty;
                _lblPowerInfo.Text = $"Power: {t.FinalPower}% | Pole: {t.PoleName} | Bait: {t.BaitName} | Bobbers: {t.ActiveBobbers}{bitePart}";

                string status = t.QuestFinishedToday ? "Done Today" : "Available";
                _lblQuestInfo.Text = $"Angler Quest: {t.QuestFishName} [#{t.QuestFishId}] ({status}) | Total Done: {t.QuestsCompletedTotal}";
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingTab.RefreshTelemetry", ex);
            }
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkMultiBobber.Checked = cfg.FishingMultiBobberEnabled;
            _barBobberCount.Value = Math.Max(_barBobberCount.Minimum, Math.Min(_barBobberCount.Maximum, cfg.FishingBobberCount));
            _barSpread.Value = Math.Max(_barSpread.Minimum, Math.Min(_barSpread.Maximum, cfg.FishingCastSpreadPercent));
            _chkInstantBite.Checked = cfg.FishingInstantBite;
            _chkAutoReel.Checked = cfg.FishingAutoReel;
            _chkAutoRecast.Checked = cfg.FishingAutoRecast;
            _cmbReelFilter.SelectedIndex = Math.Max(0, Math.Min(3, cfg.FishingAutoReelFilter));
            _chkNeverBreak.Checked = cfg.FishingNeverBreakLine;
            _chkInfBait.Checked = cfg.FishingInfiniteBait;
            _chkVirtualBait.Checked = cfg.FishingVirtualBait;
            _barPowerBonus.Value = Math.Max(_barPowerBonus.Minimum, Math.Min(_barPowerBonus.Maximum, cfg.FishingPowerBonus));
            _barCrateChance.Value = Math.Max(_barCrateChance.Minimum, Math.Min(_barCrateChance.Maximum, cfg.FishingCrateChance));
            _chkGuaranteedCrate.Checked = cfg.FishingGuaranteedCrate;
            _chkBoostRare.Checked = cfg.FishingBoostRare;
            _chkIgnorePond.Checked = cfg.FishingIgnorePondSize;
            _chkLavaFish.Checked = cfg.FishingLavaEnabled;
            _chkSonarCrate.Checked = cfg.FishingSonarAndCrate;
            _chkMultiQuest.Checked = cfg.FishingAllowMultipleQuestFish;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class PotionsTab : Panel
    {
        private readonly CheckBox _chkInfDuration;
        private readonly CheckBox _chkPurgeDebuffs;
        private readonly CheckBox _chkNoCooldown;
        private readonly CheckBox _chkInfPotions;
        private readonly CheckBox _chkSuperHeal;
        private readonly CheckBox _chkOpEnable;
        private readonly TrackBar _barOpMult;
        private readonly CheckBox _chkOpIronskin;
        private readonly CheckBox _chkOpEndurance;
        private readonly CheckBox _chkOpSwiftness;
        private readonly CheckBox _chkOpDamage;
        private readonly CheckBox _chkOpRegen;
        private readonly CheckBox _chkOpLifeforce;
        private readonly CheckBox _chkOpMining;

        private readonly TextBox _txtSearch;
        private readonly ListBox _lstBuffs;
        private readonly NumericUpDown _numMinutes;
        private List<CatalogEntry>? _buffCatalog;

        public PotionsTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpRules = DarkTheme.CreateGroup("Potion Rules", 8, 8, 256, 174);
            _chkInfDuration = DarkTheme.CreateCheck("Infinite Buff Duration", 12, 24, cfg.InfiniteBuffDuration, v => cfg.InfiniteBuffDuration = v);
            _chkPurgeDebuffs = DarkTheme.CreateCheck("Auto-Purge Debuffs", 12, 50, cfg.AutoPurgeDebuffs, v => cfg.AutoPurgeDebuffs = v);
            _chkNoCooldown = DarkTheme.CreateCheck("No Potion / Mana Sickness", 12, 76, cfg.NoPotionCooldown, v => cfg.NoPotionCooldown = v);
            _chkInfPotions = DarkTheme.CreateCheck("Infinite Potions (No Consume)", 12, 102, cfg.InfinitePotions, v => cfg.InfinitePotions = v);
            _chkSuperHeal = DarkTheme.CreateCheck("Full Heal / Mana Potions", 12, 128, cfg.SuperHealAndManaPotions, v => cfg.SuperHealAndManaPotions = v);
            grpRules.Controls.AddRange(new Control[] { _chkInfDuration, _chkPurgeDebuffs, _chkNoCooldown, _chkInfPotions, _chkSuperHeal });
            Controls.Add(grpRules);

            var grpOp = DarkTheme.CreateGroup("Potion Amplifier", 272, 8, 274, 174);
            _chkOpEnable = DarkTheme.CreateCheck("Enable Potion Scaling", 12, 22, cfg.OpPotionsEnabled, v => cfg.OpPotionsEnabled = v);
            _chkOpEnable.ForeColor = DarkTheme.AccentAmber;
            _chkOpEnable.Font = DarkTheme.FontBold;
            grpOp.Controls.Add(_chkOpEnable);

            (_barOpMult, _) = DarkTheme.CreateSliderRow(grpOp, "Power Multiplier", 12, 46, 250, 1, 20, (int)cfg.OpPotionMultiplier,
                v => $"{v}x", v => cfg.OpPotionMultiplier = v);

            _chkOpIronskin = DarkTheme.CreateCheck("Ironskin", 12, 92, cfg.OpIronskin, v => cfg.OpIronskin = v);
            _chkOpEndurance = DarkTheme.CreateCheck("Endurance", 96, 92, cfg.OpEndurance, v => cfg.OpEndurance = v);
            _chkOpSwiftness = DarkTheme.CreateCheck("Swiftness", 188, 92, cfg.OpSwiftness, v => cfg.OpSwiftness = v);
            _chkOpDamage = DarkTheme.CreateCheck("Wrath/Rage/Dmg", 12, 116, cfg.OpDamagePotions, v => cfg.OpDamagePotions = v);
            _chkOpRegen = DarkTheme.CreateCheck("Regen", 150, 116, cfg.OpRegeneration, v => cfg.OpRegeneration = v);
            _chkOpLifeforce = DarkTheme.CreateCheck("Lifeforce (+MaxHP)", 12, 140, cfg.OpLifeforce, v => cfg.OpLifeforce = v);
            _chkOpMining = DarkTheme.CreateCheck("Mining/Build", 150, 140, cfg.OpMiningBuilder, v => cfg.OpMiningBuilder = v);
            grpOp.Controls.AddRange(new Control[] { _chkOpIronskin, _chkOpEndurance, _chkOpSwiftness, _chkOpDamage, _chkOpRegen, _chkOpLifeforce, _chkOpMining });
            Controls.Add(grpOp);

            var grpPacks = DarkTheme.CreateGroup("Buff Packs", 8, 188, 538, 66);
            var btnCombat = DarkTheme.CreateButton("Combat Pack (15)", 12, 24, 184, 30, DarkTheme.AccentPurple,
                () => GameActions.EnqueueApplyCombatPack(GetDurationTicks()));
            var btnExplore = DarkTheme.CreateButton("Explorer Pack (12)", 204, 24, 178, 30, DarkTheme.AccentBlue,
                () => GameActions.EnqueueApplyExplorerPack(GetDurationTicks()));
            var btnPurge = DarkTheme.CreateButton("Clear Debuffs", 390, 24, 136, 30, DarkTheme.AccentRed,
                GameActions.EnqueuePurgeDebuffs);
            grpPacks.Controls.AddRange(new Control[] { btnCombat, btnExplore, btnPurge });
            Controls.Add(grpPacks);

            var grpInject = DarkTheme.CreateGroup("Buff Injector", 8, 260, 538, 198);
            var lblFind = new Label { Text = "Search:", Location = new Point(12, 25), Size = new Size(52, 18), ForeColor = DarkTheme.TextSecondary };
            _txtSearch = DarkTheme.CreateTextBox(64, 22, 200);
            _txtSearch.TextChanged += (_, _) => FilterBuffs();

            var btnLoad = DarkTheme.CreateButton("Load Buffs", 270, 21, 102, 24, DarkTheme.BgInput, EnsureCatalogLoaded);

            var lblMin = new Label { Text = "Mins:", Location = new Point(380, 25), Size = new Size(38, 18), ForeColor = DarkTheme.TextSecondary };
            _numMinutes = DarkTheme.CreateNumeric(420, 22, 54, 1, 999, 15);
            var btnApply = DarkTheme.CreateButton("Apply", 480, 21, 48, 24, DarkTheme.AccentGreen, InjectSelectedBuff);

            _lstBuffs = new ListBox
            {
                Location = new Point(12, 52),
                Size = new Size(514, 134),
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = DarkTheme.FontMono
            };
            _lstBuffs.DoubleClick += (_, _) => InjectSelectedBuff();

            grpInject.Controls.AddRange(new Control[] { lblFind, _txtSearch, btnLoad, lblMin, _numMinutes, btnApply, _lstBuffs });
            Controls.Add(grpInject);
        }

        private int GetDurationTicks() => (int)_numMinutes.Value * 60 * 60;

        public void EnsureCatalogLoaded()
        {
            if (_buffCatalog == null || _buffCatalog.Count == 0)
            {
                _buffCatalog = GameActions.BuildBuffCatalog();
                FilterBuffs();
            }
        }

        private void FilterBuffs()
        {
            if (_buffCatalog == null) return;
            string q = _txtSearch.Text.Trim();
            _lstBuffs.BeginUpdate();
            _lstBuffs.Items.Clear();
            IEnumerable<CatalogEntry> results = string.IsNullOrEmpty(q)
                ? _buffCatalog
                : _buffCatalog.Where(b =>
                    b.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.Id.ToString().Contains(q));

            foreach (var entry in results.Take(150))
            {
                _lstBuffs.Items.Add(entry);
            }
            _lstBuffs.EndUpdate();
        }

        private void InjectSelectedBuff()
        {
            if (_lstBuffs.SelectedItem is CatalogEntry entry)
            {
                GameActions.EnqueueApplyBuff(entry.Id, GetDurationTicks());
            }
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkInfDuration.Checked = cfg.InfiniteBuffDuration;
            _chkPurgeDebuffs.Checked = cfg.AutoPurgeDebuffs;
            _chkNoCooldown.Checked = cfg.NoPotionCooldown;
            _chkInfPotions.Checked = cfg.InfinitePotions;
            _chkSuperHeal.Checked = cfg.SuperHealAndManaPotions;
            _chkOpEnable.Checked = cfg.OpPotionsEnabled;
            _barOpMult.Value = Math.Max(_barOpMult.Minimum, Math.Min(_barOpMult.Maximum, (int)cfg.OpPotionMultiplier));
            _chkOpIronskin.Checked = cfg.OpIronskin;
            _chkOpEndurance.Checked = cfg.OpEndurance;
            _chkOpSwiftness.Checked = cfg.OpSwiftness;
            _chkOpDamage.Checked = cfg.OpDamagePotions;
            _chkOpRegen.Checked = cfg.OpRegeneration;
            _chkOpLifeforce.Checked = cfg.OpLifeforce;
            _chkOpMining.Checked = cfg.OpMiningBuilder;
        }
    }
}

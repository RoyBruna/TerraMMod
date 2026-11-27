using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Terraria.ID;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class EntitiesChaosTab : Panel
    {
        private readonly Label _lblTelemetry;
        private readonly TextBox _txtSearch;
        private readonly ListBox _lstNpcs;
        private readonly ComboBox _cmbLocation;
        private readonly NumericUpDown _numCount;
        private readonly Label _lblSelectedNpc;
        private readonly CheckBox _chkF4Hotkey;

        private readonly Button _btnCatAll;
        private readonly Button _btnCatBoss;
        private readonly Button _btnCatMob;
        private readonly Button _btnCatTown;
        private readonly Button _btnCatCrit;

        private readonly TrackBar _barSpawnRate;
        private readonly CheckBox _chkFreeze;
        private readonly CheckBox _chkMagnet;
        private readonly CheckBox _chkVacuum;

        private List<NpcCatalogEntry>? _catalog;
        private int _categoryFilter = -1;

        public EntitiesChaosTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpTel = DarkTheme.CreateGroup("Live World Entities Telemetry", 8, 8, 538, 46);
            _lblTelemetry = new Label
            {
                Text = "Hostiles: 0 | Bosses: 0 | Town: 0 | Ground Items: 0/400 | Spawn: 1x",
                Location = new Point(12, 20),
                Size = new Size(514, 18),
                ForeColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };
            grpTel.Controls.Add(_lblTelemetry);
            Controls.Add(grpTel);

            var grpBoss = DarkTheme.CreateGroup("Quick Boss Spawner (1-Click)", 8, 60, 538, 86);
            AddBossBtn(grpBoss, "King Slime", 12, 22, DarkTheme.AccentBlue, NPCID.KingSlime);
            AddBossBtn(grpBoss, "Eye Cthulhu", 98, 22, DarkTheme.AccentRed, NPCID.EyeofCthulhu);
            AddBossBtn(grpBoss, "Queen Bee", 184, 22, DarkTheme.AccentAmber, NPCID.QueenBee, true);
            AddBossBtn(grpBoss, "Skeletron", 270, 22, DarkTheme.BgInput, NPCID.SkeletronHead);
            AddBossBtn(grpBoss, "Wall Flesh", 356, 22, DarkTheme.AccentRed, NPCID.WallofFlesh);
            AddBossBtn(grpBoss, "Queen Slime", 442, 22, DarkTheme.AccentPurple, NPCID.QueenSlimeBoss);

            AddBossBtn(grpBoss, "The Twins", 12, 52, DarkTheme.AccentGreen, -999);
            AddBossBtn(grpBoss, "Destroyer", 98, 52, DarkTheme.BgInput, NPCID.TheDestroyer);
            AddBossBtn(grpBoss, "S. Prime", 184, 52, DarkTheme.BgInput, NPCID.SkeletronPrime);
            AddBossBtn(grpBoss, "Plantera", 270, 52, DarkTheme.AccentGreen, NPCID.Plantera);
            AddBossBtn(grpBoss, "Golem", 356, 52, DarkTheme.AccentAmber, NPCID.Golem, true);
            AddBossBtn(grpBoss, "Moon Lord", 442, 52, DarkTheme.AccentCyan, NPCID.MoonLordCore, true);
            Controls.Add(grpBoss);

            var grpSpawn = DarkTheme.CreateGroup("Custom NPC & Creature Spawner (Double-Click or Press F4)", 8, 152, 538, 232);

            _txtSearch = DarkTheme.CreateTextBox(12, 22, 212);
            _txtSearch.TextChanged += (_, _) => FilterCatalog();

            _btnCatAll = DarkTheme.CreateButton("All", 230, 21, 52, 24, DarkTheme.AccentCyan, () => SetCategoryFilter(-1));
            _btnCatBoss = DarkTheme.CreateButton("Boss", 286, 21, 56, 24, DarkTheme.BgInput, () => SetCategoryFilter((int)NpcCatalogCategory.Boss));
            _btnCatMob = DarkTheme.CreateButton("Mob", 346, 21, 56, 24, DarkTheme.BgInput, () => SetCategoryFilter((int)NpcCatalogCategory.Hostile));
            _btnCatTown = DarkTheme.CreateButton("Town", 406, 21, 58, 24, DarkTheme.BgInput, () => SetCategoryFilter((int)NpcCatalogCategory.Town));
            _btnCatCrit = DarkTheme.CreateButton("Crit", 468, 21, 58, 24, DarkTheme.BgInput, () => SetCategoryFilter((int)NpcCatalogCategory.Critter));
            UpdateCategoryStyles();

            _lstNpcs = new ListBox
            {
                Location = new Point(12, 50),
                Size = new Size(514, 110),
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = DarkTheme.FontMono
            };
            _lstNpcs.SelectedIndexChanged += (_, _) => OnNpcSelectionChanged();
            _lstNpcs.DoubleClick += (_, _) => SpawnSelectedFromList();

            var lblWhere = new Label
            {
                Text = "At:",
                Location = new Point(12, 170),
                Size = new Size(24, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal,
                UseMnemonic = false
            };

            _cmbLocation = new ComboBox
            {
                Location = new Point(36, 166),
                Size = new Size(124, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontNormal
            };
            _cmbLocation.Items.AddRange(new object[]
            {
                "Near Player",
                "At Cursor",
                "Native Spawn"
            });
            _cmbLocation.SelectedIndex = Math.Max(0, Math.Min(2, cfg.SelectedSpawnLocationMode));
            _cmbLocation.SelectedIndexChanged += (_, _) =>
            {
                if (_cmbLocation.SelectedIndex >= 0)
                {
                    cfg.SelectedSpawnLocationMode = _cmbLocation.SelectedIndex;
                }
            };

            var lblQty = new Label
            {
                Text = "Qty:",
                Location = new Point(166, 170),
                Size = new Size(30, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal,
                UseMnemonic = false
            };

            _numCount = DarkTheme.CreateNumeric(198, 167, 54, 1, 100, cfg.SelectedSpawnNpcCount);
            _numCount.ValueChanged += (_, _) => cfg.SelectedSpawnNpcCount = (int)_numCount.Value;

            var btnQ1 = DarkTheme.CreateButton("1", 256, 166, 30, 24, DarkTheme.BgInput, () => _numCount.Value = 1);
            var btnQ5 = DarkTheme.CreateButton("5", 290, 166, 30, 24, DarkTheme.BgInput, () => _numCount.Value = 5);
            var btnQ10 = DarkTheme.CreateButton("10", 324, 166, 36, 24, DarkTheme.BgInput, () => _numCount.Value = 10);
            var btnQ25 = DarkTheme.CreateButton("25", 364, 166, 36, 24, DarkTheme.BgInput, () => _numCount.Value = 25);

            var btnSpawnNow = DarkTheme.CreateButton("Spawn NPC", 406, 165, 120, 26, DarkTheme.AccentGreen, SpawnSelectedFromList);

            _chkF4Hotkey = DarkTheme.CreateCheck("F4 -> Spawn Selected at Cursor", 12, 200, cfg.HotkeySpawnNpcAtCursor, v => cfg.HotkeySpawnNpcAtCursor = v);
            _chkF4Hotkey.ForeColor = DarkTheme.AccentAmber;
            _chkF4Hotkey.Font = DarkTheme.FontBold;

            _lblSelectedNpc = new Label
            {
                Text = "Selected: Eye of Cthulhu [#4]",
                Location = new Point(252, 201),
                Size = new Size(274, 18),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontMono,
                UseMnemonic = false
            };

            grpSpawn.Controls.AddRange(new Control[]
            {
                _txtSearch, _btnCatAll, _btnCatBoss, _btnCatMob, _btnCatTown, _btnCatCrit,
                _lstNpcs, lblWhere, _cmbLocation, lblQty, _numCount,
                btnQ1, btnQ5, btnQ10, btnQ25, btnSpawnNow, _chkF4Hotkey, _lblSelectedNpc
            });
            Controls.Add(grpSpawn);

            var grpChaos = DarkTheme.CreateGroup("Creature Spawn Rate (0x - 100x) & Chaos Controls", 8, 390, 538, 174);

            (_barSpawnRate, _) = DarkTheme.CreateSliderRow(grpChaos, "Creature Spawn Rate", 12, 22, 244, 0, 100, cfg.SpawnRateMultiplier,
                v => v == 0 ? "0x (OFF)" : $"{v}x", v => cfg.SpawnRateMultiplier = v);

            var btnS0 = DarkTheme.CreateButton("0x", 12, 68, 44, 24, DarkTheme.BgInput, () => SetSpawnRate(0));
            var btnS1 = DarkTheme.CreateButton("1x", 60, 68, 44, 24, DarkTheme.BgInput, () => SetSpawnRate(1));
            var btnS10 = DarkTheme.CreateButton("10x", 108, 68, 46, 24, DarkTheme.AccentBlue, () => SetSpawnRate(10));
            var btnS50 = DarkTheme.CreateButton("50x", 158, 68, 46, 24, DarkTheme.AccentPurple, () => SetSpawnRate(50));
            var btnS100 = DarkTheme.CreateButton("100x", 208, 68, 48, 24, DarkTheme.AccentRed, () => SetSpawnRate(100));

            var btnButcher = DarkTheme.CreateButton("Butcher Hostiles (With Loot)", 12, 102, 244, 28, DarkTheme.AccentRed,
                () => GameActions.EnqueueButcherHostiles(false));
            var btnButcherAll = DarkTheme.CreateButton("Butcher ALL (+ Bosses)", 12, 136, 244, 28, DarkTheme.AccentPurple,
                () => GameActions.EnqueueButcherHostiles(true));

            _chkFreeze = DarkTheme.CreateCheck("Freeze All Hostile Enemies", 280, 24, cfg.ChaosFreezeEnemies, v => cfg.ChaosFreezeEnemies = v);
            _chkFreeze.ForeColor = DarkTheme.AccentCyan;
            _chkFreeze.Font = DarkTheme.FontBold;

            _chkMagnet = DarkTheme.CreateCheck("Enemy Magnet to Cursor", 280, 50, cfg.ChaosEnemyMagnetToCursor, v => cfg.ChaosEnemyMagnetToCursor = v);
            _chkMagnet.ForeColor = DarkTheme.AccentAmber;
            _chkMagnet.Font = DarkTheme.FontBold;

            _chkVacuum = DarkTheme.CreateCheck("Global Item Vacuum (All Loot)", 280, 76, cfg.ChaosItemVacuum, v => cfg.ChaosItemVacuum = v);
            _chkVacuum.ForeColor = DarkTheme.AccentGreen;
            _chkVacuum.Font = DarkTheme.FontBold;

            var btnClearItems = DarkTheme.CreateButton("Clear Ground Items", 280, 102, 244, 28, DarkTheme.BgInput, GameActions.EnqueueClearGroundItems);
            var btnResetChaos = DarkTheme.CreateButton("Reset Chaos Defaults", 280, 136, 244, 28, DarkTheme.BgInput, () =>
            {
                ModConfig.Instance.ResetChaosOnly();
                SyncFromConfig();
            });

            grpChaos.Controls.AddRange(new Control[]
            {
                btnS0, btnS1, btnS10, btnS50, btnS100, btnButcher, btnButcherAll,
                _chkFreeze, _chkMagnet, _chkVacuum, btnClearItems, btnResetChaos
            });
            Controls.Add(grpChaos);
        }

        private void AddBossBtn(Control parent, string text, int x, int y, Color bg, int npcType, bool darkText = false)
        {
            var btn = DarkTheme.CreateButton(text, x, y, 82, 26, bg, () =>
            {
                GameActions.EnqueueSpawnNpc(npcType, 1, ModConfig.Instance.SelectedSpawnLocationMode);
            });
            btn.Font = DarkTheme.FontTiny;
            if (darkText)
            {
                btn.ForeColor = Color.Black;
            }
            parent.Controls.Add(btn);
        }

        public void OnTabSelected()
        {
            EnsureCatalogLoaded();
            SyncFromConfig();
            RefreshTelemetry();
        }

        public void EnsureCatalogLoaded()
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                _catalog = GameActions.BuildNpcCatalog();
                FilterCatalog();
            }
        }

        private void SetCategoryFilter(int category)
        {
            _categoryFilter = category;
            UpdateCategoryStyles();
            FilterCatalog();
        }

        private void UpdateCategoryStyles()
        {
            StyleCatBtn(_btnCatAll, _categoryFilter == -1);
            StyleCatBtn(_btnCatBoss, _categoryFilter == (int)NpcCatalogCategory.Boss);
            StyleCatBtn(_btnCatMob, _categoryFilter == (int)NpcCatalogCategory.Hostile);
            StyleCatBtn(_btnCatTown, _categoryFilter == (int)NpcCatalogCategory.Town);
            StyleCatBtn(_btnCatCrit, _categoryFilter == (int)NpcCatalogCategory.Critter);
        }

        private static void StyleCatBtn(Button btn, bool active)
        {
            btn.BackColor = active ? DarkTheme.AccentCyan : DarkTheme.BgInput;
            btn.ForeColor = active ? Color.Black : DarkTheme.TextPrimary;
        }

        private void FilterCatalog()
        {
            if (_catalog == null)
            {
                return;
            }

            string q = _txtSearch.Text.Trim();
            _lstNpcs.BeginUpdate();
            _lstNpcs.Items.Clear();

            IEnumerable<NpcCatalogEntry> query = _catalog;
            if (_categoryFilter >= 0)
            {
                NpcCatalogCategory cat = (NpcCatalogCategory)_categoryFilter;
                query = query.Where(n => n.Category == cat);
            }

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(n =>
                    n.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.Id.ToString().Contains(q));
            }

            foreach (NpcCatalogEntry entry in query.Take(200))
            {
                _lstNpcs.Items.Add(entry);
            }

            _lstNpcs.EndUpdate();
        }

        private void OnNpcSelectionChanged()
        {
            if (_lstNpcs.SelectedItem is NpcCatalogEntry entry)
            {
                ModConfig.Instance.SelectedSpawnNpcId = entry.Id;
                _lblSelectedNpc.Text = $"Selected: {entry.Name} [#{entry.Id}]";
            }
        }

        private void SpawnSelectedFromList()
        {
            int npcId = _lstNpcs.SelectedItem is NpcCatalogEntry entry
                ? entry.Id
                : ModConfig.Instance.SelectedSpawnNpcId;

            if (npcId > 0 || npcId == -999)
            {
                GameActions.EnqueueSpawnNpc(npcId, (int)_numCount.Value, ModConfig.Instance.SelectedSpawnLocationMode);
            }
        }

        private void SetSpawnRate(int rate)
        {
            int clamped = Math.Max(_barSpawnRate.Minimum, Math.Min(_barSpawnRate.Maximum, rate));
            _barSpawnRate.Value = clamped;
            ModConfig.Instance.SpawnRateMultiplier = clamped;
        }

        public void RefreshTelemetry()
        {
            try
            {
                EntityChaosTelemetry t = EntityChaosEngine.CaptureTelemetry();
                if (!t.InWorld)
                {
                    _lblTelemetry.Text = $"Hostiles: - | Bosses: - | Town: - | Ground Items: - | Spawn: {ModConfig.Instance.SpawnRateMultiplier}x";
                    return;
                }

                string rateStr = t.SpawnRateMult == 0 ? "OFF" : $"{t.SpawnRateMult}x";
                _lblTelemetry.Text = $"Hostiles: {t.HostileCount} | Bosses: {t.BossCount} | Town: {t.TownCount} | Items: {t.GroundItemCount}/400 | Spawn: {rateStr}";
            }
            catch (Exception ex)
            {
                ModLogger.Error("EntitiesChaosTab.RefreshTelemetry", ex);
            }
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _barSpawnRate.Value = Math.Max(_barSpawnRate.Minimum, Math.Min(_barSpawnRate.Maximum, cfg.SpawnRateMultiplier));
            _chkFreeze.Checked = cfg.ChaosFreezeEnemies;
            _chkMagnet.Checked = cfg.ChaosEnemyMagnetToCursor;
            _chkVacuum.Checked = cfg.ChaosItemVacuum;
            _chkF4Hotkey.Checked = cfg.HotkeySpawnNpcAtCursor;
            _cmbLocation.SelectedIndex = Math.Max(0, Math.Min(2, cfg.SelectedSpawnLocationMode));
            _numCount.Value = Math.Max(_numCount.Minimum, Math.Min(_numCount.Maximum, cfg.SelectedSpawnNpcCount));
        }
    }
}

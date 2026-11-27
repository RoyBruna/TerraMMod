using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class SpawnerWorldTab : Panel
    {
        private readonly TextBox _txtSearch;
        private readonly ListBox _lstItems;
        private readonly NumericUpDown _numStack;
        private readonly ComboBox _cmbPrefix;
        private List<CatalogEntry>? _itemCatalog;

        private readonly CheckBox _chkFullbright;
        private readonly CheckBox _chkSpelunker;
        private readonly CheckBox _chkHunter;
        private readonly CheckBox _chkDanger;
        private readonly CheckBox _chkNightVis;
        private readonly CheckBox _chkFreezeTime;
        private readonly CheckBox _chkTpHotkey;
        private readonly TrackBar _barDayRate;
        private readonly TrackBar _barSpawnRate;

        public SpawnerWorldTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpSpawn = DarkTheme.CreateGroup("Item Spawner", 8, 8, 538, 224);
            var lblFind = new Label { Text = "Item:", Location = new Point(12, 25), Size = new Size(42, 18), ForeColor = DarkTheme.TextSecondary };
            _txtSearch = DarkTheme.CreateTextBox(54, 22, 234);
            _txtSearch.TextChanged += (_, _) => FilterItems();

            var btnLoad = DarkTheme.CreateButton("Load Items", 294, 21, 110, 24, DarkTheme.BgInput, EnsureCatalogLoaded);
            var btnSpawn = DarkTheme.CreateButton("Spawn", 412, 21, 114, 24, DarkTheme.AccentGreen, SpawnSelectedItem);

            _lstItems = new ListBox
            {
                Location = new Point(12, 52),
                Size = new Size(514, 126),
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = DarkTheme.FontMono
            };
            _lstItems.DoubleClick += (_, _) => SpawnSelectedItem();

            var lblStack = new Label { Text = "Stack:", Location = new Point(12, 188), Size = new Size(44, 18), ForeColor = DarkTheme.TextSecondary };
            _numStack = DarkTheme.CreateNumeric(58, 185, 76, 1, 9999, 1);

            var lblPrefix = new Label { Text = "Prefix:", Location = new Point(146, 188), Size = new Size(46, 18), ForeColor = DarkTheme.TextSecondary };
            _cmbPrefix = new ComboBox
            {
                Location = new Point(194, 185),
                Size = new Size(210, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontNormal
            };
            foreach (var p in GameActions.BuildPrefixCatalog())
            {
                _cmbPrefix.Items.Add(p);
            }
            _cmbPrefix.SelectedIndex = 0;

            var btnMaxStack = DarkTheme.CreateButton("Max (9999)", 412, 184, 114, 24, DarkTheme.AccentBlue, () => _numStack.Value = 9999);

            grpSpawn.Controls.AddRange(new Control[]
            {
                lblFind, _txtSearch, btnLoad, btnSpawn, _lstItems,
                lblStack, _numStack, lblPrefix, _cmbPrefix, btnMaxStack
            });
            Controls.Add(grpSpawn);

            var grpVis = DarkTheme.CreateGroup("Visuals & Map", 8, 238, 250, 228);
            _chkFullbright = DarkTheme.CreateCheck("Fullbright", 12, 24, cfg.Fullbright, v => cfg.Fullbright = v);
            _chkSpelunker = DarkTheme.CreateCheck("Spelunker (Ores & Chests)", 12, 50, cfg.VisionSpelunker, v => cfg.VisionSpelunker = v);
            _chkHunter = DarkTheme.CreateCheck("Hunter (Enemies)", 12, 76, cfg.VisionHunter, v => cfg.VisionHunter = v);
            _chkDanger = DarkTheme.CreateCheck("Dangersense (Traps)", 12, 102, cfg.VisionDangerSense, v => cfg.VisionDangerSense = v);
            _chkNightVis = DarkTheme.CreateCheck("Night Vision", 12, 128, cfg.VisionNightVision, v => cfg.VisionNightVision = v);

            var btnReveal = DarkTheme.CreateButton("Reveal Full Map", 12, 158, 226, 30, DarkTheme.AccentAmber, GameActions.EnqueueRevealFullMap);
            btnReveal.ForeColor = Color.Black;
            var btnClearMap = DarkTheme.CreateButton("Clear / Reset Map", 12, 192, 226, 26, DarkTheme.BgInput, GameActions.EnqueueClearMap);

            grpVis.Controls.AddRange(new Control[] { _chkFullbright, _chkSpelunker, _chkHunter, _chkDanger, _chkNightVis, btnReveal, btnClearMap });
            Controls.Add(grpVis);

            var grpWorld = DarkTheme.CreateGroup("World, Time & Teleports", 266, 238, 280, 228);

            var btnDawn = DarkTheme.CreateButton("Dawn", 12, 24, 60, 24, DarkTheme.BgInput, () => GameActions.EnqueueSetTime(true, 0));
            var btnNoon = DarkTheme.CreateButton("Noon", 76, 24, 60, 24, DarkTheme.AccentBlue, () => GameActions.EnqueueSetTime(true, 27000));
            var btnDusk = DarkTheme.CreateButton("Dusk", 140, 24, 60, 24, DarkTheme.BgInput, () => GameActions.EnqueueSetTime(false, 0));
            var btnMid = DarkTheme.CreateButton("Night", 204, 24, 64, 24, DarkTheme.AccentPurple, () => GameActions.EnqueueSetTime(false, 16200));

            _chkFreezeTime = DarkTheme.CreateCheck("Freeze World Time", 12, 54, cfg.FreezeTime, v => cfg.FreezeTime = v);
            grpWorld.Controls.AddRange(new Control[] { btnDawn, btnNoon, btnDusk, btnMid, _chkFreezeTime });

            (_barDayRate, _) = DarkTheme.CreateSliderRow(grpWorld, "Time Speed (dayRate)", 12, 78, 256, 1, 60, cfg.CustomDayRate,
                v => $"{v}x", v => cfg.CustomDayRate = v);

            (_barSpawnRate, _) = DarkTheme.CreateSliderRow(grpWorld, "Enemy Spawn Rate", 12, 124, 256, 0, 100, cfg.SpawnRateMultiplier,
                v => v == 0 ? "0x (OFF)" : $"{v}x", v => cfg.SpawnRateMultiplier = v);

            var btnTpSpawn = DarkTheme.CreateButton("TP Spawn", 12, 172, 122, 24, DarkTheme.AccentCyan, GameActions.EnqueueTeleportToSpawn);
            btnTpSpawn.ForeColor = Color.Black;
            var btnTpCursor = DarkTheme.CreateButton("TP Cursor", 142, 172, 126, 24, DarkTheme.AccentCyan, GameActions.EnqueueTeleportToCursor);
            btnTpCursor.ForeColor = Color.Black;
            _chkTpHotkey = DarkTheme.CreateCheck("F3 Hotkey -> Teleport to Cursor", 12, 202, cfg.HotkeyTeleportCursor, v => cfg.HotkeyTeleportCursor = v);

            grpWorld.Controls.AddRange(new Control[] { btnTpSpawn, btnTpCursor, _chkTpHotkey });
            Controls.Add(grpWorld);
        }

        public void EnsureCatalogLoaded()
        {
            if (_itemCatalog == null || _itemCatalog.Count == 0)
            {
                _itemCatalog = GameActions.BuildItemCatalog();
                FilterItems();
            }
        }

        private void FilterItems()
        {
            if (_itemCatalog == null) return;
            string q = _txtSearch.Text.Trim();
            _lstItems.BeginUpdate();
            _lstItems.Items.Clear();

            IEnumerable<CatalogEntry> results = string.IsNullOrEmpty(q)
                ? _itemCatalog
                : _itemCatalog.Where(it =>
                    it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    it.Id.ToString().Contains(q));

            foreach (var entry in results.Take(150))
            {
                _lstItems.Items.Add(entry);
            }
            _lstItems.EndUpdate();
        }

        private void SpawnSelectedItem()
        {
            if (_lstItems.SelectedItem is CatalogEntry entry)
            {
                int prefix = _cmbPrefix.SelectedItem is CatalogEntry p ? p.Id : 0;
                GameActions.EnqueueSpawnItem(entry.Id, (int)_numStack.Value, prefix);
            }
        }

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkFullbright.Checked = cfg.Fullbright;
            _chkSpelunker.Checked = cfg.VisionSpelunker;
            _chkHunter.Checked = cfg.VisionHunter;
            _chkDanger.Checked = cfg.VisionDangerSense;
            _chkNightVis.Checked = cfg.VisionNightVision;
            _chkFreezeTime.Checked = cfg.FreezeTime;
            _chkTpHotkey.Checked = cfg.HotkeyTeleportCursor;
            _barDayRate.Value = Math.Max(_barDayRate.Minimum, Math.Min(_barDayRate.Maximum, cfg.CustomDayRate));
            _barSpawnRate.Value = Math.Max(_barSpawnRate.Minimum, Math.Min(_barSpawnRate.Maximum, cfg.SpawnRateMultiplier));
        }
    }
}

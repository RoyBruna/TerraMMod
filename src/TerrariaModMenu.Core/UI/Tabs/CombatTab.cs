using System;
using System.Drawing;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class CombatTab : Panel
    {
        private readonly CheckBox _chkMelee;
        private readonly CheckBox _chkRanged;
        private readonly CheckBox _chkMagic;
        private readonly CheckBox _chkThrow;
        private readonly CheckBox _chkTools;
        private readonly CheckBox _chkBlocks;

        private readonly CheckBox _chkAutoReuse;
        private readonly CheckBox _chkInfAmmo;
        private readonly CheckBox _chkInfThrow;
        private readonly CheckBox _chkInfBlocks;

        private readonly CheckBox _chkMultishot;
        private readonly CheckBox _chkHoming;
        private readonly CheckBox _chkWallPierce;
        private readonly CheckBox _chkInfPierce;
        private readonly TrackBar _barMultiCount;
        private readonly TrackBar _barMultiSpread;
        private readonly TrackBar _barHomingRange;
        private readonly TrackBar _barHomingTurn;
        private readonly TrackBar _barProjSpeed;

        private readonly Label _lblHeldInfo;
        private readonly NumericUpDown _numDmg;
        private readonly NumericUpDown _numCrit;
        private readonly NumericUpDown _numUseTime;
        private readonly NumericUpDown _numUseAnim;
        private readonly NumericUpDown _numShootSpd;
        private readonly NumericUpDown _numKb;
        private readonly NumericUpDown _numScale;
        private readonly CheckBox _chkHeldAuto;

        public CombatTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var cfg = ModConfig.Instance;

            var grpRapid = DarkTheme.CreateGroup("Rapid Fire by Category (ticks/use)", 8, 8, 538, 224);

            _chkMelee = AddRapidRow(grpRapid, "Melee (Swords, Spears, Yoyos)", 12, 24, cfg.RapidFireMelee, cfg.RapidFireMeleeTicks,
                v => cfg.RapidFireMelee = v, t => cfg.RapidFireMeleeTicks = t);
            _chkRanged = AddRapidRow(grpRapid, "Ranged (Bows, Guns, Launchers)", 12, 56, cfg.RapidFireRanged, cfg.RapidFireRangedTicks,
                v => cfg.RapidFireRanged = v, t => cfg.RapidFireRangedTicks = t);
            _chkMagic = AddRapidRow(grpRapid, "Magic & Summon (Staves, Tomes)", 12, 88, cfg.RapidFireMagic, cfg.RapidFireMagicTicks,
                v => cfg.RapidFireMagic = v, t => cfg.RapidFireMagicTicks = t);
            _chkThrow = AddRapidRow(grpRapid, "Throwables (Grenades, Bombs, Shurikens)", 12, 120, cfg.RapidFireThrowables, cfg.RapidFireThrowablesTicks,
                v => cfg.RapidFireThrowables = v, t => cfg.RapidFireThrowablesTicks = t);
            _chkTools = AddRapidRow(grpRapid, "Tools (Pickaxes, Axes, Hammers)", 12, 152, cfg.RapidFireTools, cfg.RapidFireToolsTicks,
                v => cfg.RapidFireTools = v, t => cfg.RapidFireToolsTicks = t);
            _chkBlocks = AddRapidRow(grpRapid, "Blocks & Walls", 12, 184, cfg.RapidFireBlocks, cfg.RapidFireBlocksTicks,
                v => cfg.RapidFireBlocks = v, t => cfg.RapidFireBlocksTicks = t);

            Controls.Add(grpRapid);

            var grpExtras = DarkTheme.CreateGroup("Ammo & Consumables", 8, 238, 538, 68);
            _chkAutoReuse = DarkTheme.CreateCheck("Force Auto-Reuse All", 12, 22, cfg.ForceAutoReuseAll, v => cfg.ForceAutoReuseAll = v);
            _chkInfAmmo = DarkTheme.CreateCheck("Infinite Ammo", 190, 22, cfg.InfiniteAmmo, v => cfg.InfiniteAmmo = v);
            _chkInfThrow = DarkTheme.CreateCheck("Infinite Throwables (Grenades/Bombs)", 12, 44, cfg.InfiniteThrowables, v => cfg.InfiniteThrowables = v);
            _chkInfBlocks = DarkTheme.CreateCheck("Infinite Blocks / Walls", 270, 44, cfg.InfiniteBlocks, v => cfg.InfiniteBlocks = v);
            grpExtras.Controls.AddRange(new Control[] { _chkAutoReuse, _chkInfAmmo, _chkInfThrow, _chkInfBlocks });
            Controls.Add(grpExtras);

            var grpEditor = DarkTheme.CreateGroup("Held Item Inspector & Editor", 8, 312, 538, 148);
            _lblHeldInfo = new Label
            {
                Text = "Held: (click Load Held Item)",
                Location = new Point(12, 22),
                Size = new Size(375, 20),
                ForeColor = DarkTheme.AccentAmber,
                Font = DarkTheme.FontMono
            };
            var btnLoad = DarkTheme.CreateButton("Load Held Item", 396, 18, 130, 26, DarkTheme.AccentBlue, LoadHeldItem);
            grpEditor.Controls.Add(_lblHeldInfo);
            grpEditor.Controls.Add(btnLoad);

            _numDmg = AddField(grpEditor, "Damage:", 12, 52, 0, 999999, 100);
            _numCrit = AddField(grpEditor, "Crit %:", 142, 52, 0, 100, 25);
            _numUseTime = AddField(grpEditor, "UseTime:", 272, 52, 1, 200, 10);
            _numUseAnim = AddField(grpEditor, "UseAnim:", 402, 52, 1, 200, 10);

            _numShootSpd = AddField(grpEditor, "ProjSpd:", 12, 82, 0, 100, 16, 1);
            _numKb = AddField(grpEditor, "KnockB:", 142, 82, 0, 100, 6, 1);
            _numScale = AddField(grpEditor, "Scale:", 272, 82, 0.2m, 10m, 1.0m, 1);
            _chkHeldAuto = new CheckBox
            {
                Text = "AutoReuse",
                Location = new Point(404, 83),
                AutoSize = true,
                Checked = true,
                ForeColor = DarkTheme.TextPrimary,
                Font = DarkTheme.FontNormal
            };
            grpEditor.Controls.Add(_chkHeldAuto);

            var btnApply = DarkTheme.CreateButton("Apply Stats", 12, 112, 168, 28, DarkTheme.AccentGreen, ApplyHeldItem);
            var btnOpPreset = DarkTheme.CreateButton("Overclock (10x)", 188, 112, 168, 28, DarkTheme.AccentPurple, MakeHeldItemOp);
            var btnReset = DarkTheme.CreateButton("Restore Defaults", 364, 112, 162, 28, DarkTheme.AccentRed, GameActions.EnqueueResetHeldItem);
            grpEditor.Controls.AddRange(new Control[] { btnApply, btnOpPreset, btnReset });

            Controls.Add(grpEditor);

            var grpProj = DarkTheme.CreateGroup("Projectile Madness (Multishot, Homing & Wall-Pierce)", 8, 466, 538, 198);

            _chkMultishot = DarkTheme.CreateCheck("Multishot / Shotgun Fan", 12, 24, cfg.ProjectileMultishotEnabled, v => cfg.ProjectileMultishotEnabled = v);
            _chkMultishot.ForeColor = DarkTheme.AccentAmber;
            _chkMultishot.Font = DarkTheme.FontBold;
            grpProj.Controls.Add(_chkMultishot);

            (_barMultiCount, _) = DarkTheme.CreateSliderRow(grpProj, "Projectiles per Shot", 12, 48, 244, 2, 10, cfg.ProjectileMultishotCount,
                v => $"{v}x", v => cfg.ProjectileMultishotCount = v);
            (_barMultiSpread, _) = DarkTheme.CreateSliderRow(grpProj, "Spread Cone Angle", 12, 92, 244, 5, 90, (int)cfg.ProjectileMultishotSpreadDeg,
                v => $"{v} deg", v => cfg.ProjectileMultishotSpreadDeg = v);

            _chkHoming = DarkTheme.CreateCheck("Universal Homing", 280, 24, cfg.ProjectileUniversalHoming, v => cfg.ProjectileUniversalHoming = v);
            _chkHoming.ForeColor = DarkTheme.AccentCyan;
            _chkHoming.Font = DarkTheme.FontBold;
            grpProj.Controls.Add(_chkHoming);

            (_barHomingRange, _) = DarkTheme.CreateSliderRow(grpProj, "Homing Lock Range", 280, 48, 244, 400, 2400, (int)cfg.ProjectileHomingRange,
                v => $"{v} px", v => cfg.ProjectileHomingRange = v);
            (_barHomingTurn, _) = DarkTheme.CreateSliderRow(grpProj, "Homing Turn Strength", 280, 92, 244, 8, 45, (int)(cfg.ProjectileHomingTurn * 100f),
                v => $"{v}%", v => cfg.ProjectileHomingTurn = v / 100f);

            _chkWallPierce = DarkTheme.CreateCheck("Wall-Pierce (Through Blocks)", 12, 142, cfg.ProjectileNoTileCollide, v => cfg.ProjectileNoTileCollide = v);
            _chkInfPierce = DarkTheme.CreateCheck("Infinite Enemy Pierce", 12, 166, cfg.ProjectileInfinitePierce, v => cfg.ProjectileInfinitePierce = v);
            grpProj.Controls.Add(_chkWallPierce);
            grpProj.Controls.Add(_chkInfPierce);

            (_barProjSpeed, _) = DarkTheme.CreateSliderRow(grpProj, "Projectile Velocity", 280, 140, 244, 10, 40, (int)(cfg.ProjectileSpeedMultiplier * 10f),
                v => $"{v / 10f:0.0}x", v => cfg.ProjectileSpeedMultiplier = v / 10f);

            Controls.Add(grpProj);
        }

        private static CheckBox AddRapidRow(
            Control parent, string label, int x, int y, bool initChecked, int initTicks,
            Action<bool> onToggle, Action<int> onTicks)
        {
            var chk = DarkTheme.CreateCheck(label, x, y + 3, initChecked, onToggle);
            var lbl = new Label
            {
                Text = $"{initTicks}t ({60 / Math.Max(1, initTicks)}/s)",
                Location = new Point(445, y + 4),
                Size = new Size(84, 18),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = DarkTheme.AccentAmber,
                Font = DarkTheme.FontMono
            };
            var bar = new TrackBar
            {
                AutoSize = false,
                Location = new Point(282, y + 1),
                Size = new Size(160, 22),
                Minimum = 1,
                Maximum = 15,
                Value = Math.Max(1, Math.Min(15, initTicks)),
                TickStyle = TickStyle.None,
                BackColor = parent.BackColor
            };
            bar.ValueChanged += (_, _) =>
            {
                lbl.Text = $"{bar.Value}t ({60 / Math.Max(1, bar.Value)}/s)";
                onTicks(bar.Value);
            };
            parent.Controls.Add(bar);
            parent.Controls.Add(chk);
            parent.Controls.Add(lbl);
            return chk;
        }

        private static NumericUpDown AddField(Control parent, string caption, int x, int y, decimal min, decimal max, decimal val, int decimals = 0)
        {
            var lbl = new Label
            {
                Text = caption,
                Location = new Point(x, y + 3),
                Size = new Size(56, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal
            };
            var num = DarkTheme.CreateNumeric(x + 56, y, 66, min, max, val, decimals);
            parent.Controls.Add(lbl);
            parent.Controls.Add(num);
            return num;
        }

        private void LoadHeldItem()
        {
            GameSnapshot snap = GameState.CaptureSnapshot();
            if (!snap.InWorld || !snap.HeldItem.Valid)
            {
                _lblHeldInfo.Text = "Held: (empty)";
                return;
            }

            var h = snap.HeldItem;
            _lblHeldInfo.Text = $"Held: {h.Name} [{h.Category}] (ID {h.Type})";
            _numDmg.Value = Clamp(_numDmg, h.Damage);
            _numCrit.Value = Clamp(_numCrit, h.Crit);
            _numUseTime.Value = Clamp(_numUseTime, Math.Max(1, h.UseTime));
            _numUseAnim.Value = Clamp(_numUseAnim, Math.Max(1, h.UseAnimation));
            _numShootSpd.Value = Clamp(_numShootSpd, (decimal)h.ShootSpeed);
            _numKb.Value = Clamp(_numKb, (decimal)h.KnockBack);
            _numScale.Value = Clamp(_numScale, (decimal)Math.Max(0.2f, h.Scale));
            _chkHeldAuto.Checked = h.AutoReuse;
        }

        private void ApplyHeldItem()
        {
            GameActions.EnqueueModifyHeldItem(
                (int)_numDmg.Value,
                (int)_numCrit.Value,
                (int)_numUseTime.Value,
                (int)_numUseAnim.Value,
                (float)_numShootSpd.Value,
                (float)_numKb.Value,
                (float)_numScale.Value,
                _chkHeldAuto.Checked);
        }

        private void MakeHeldItemOp()
        {
            LoadHeldItem();
            _numDmg.Value = Clamp(_numDmg, Math.Max(500, _numDmg.Value * 10));
            _numCrit.Value = 100;
            _numUseTime.Value = 2;
            _numUseAnim.Value = 2;
            _numShootSpd.Value = Clamp(_numShootSpd, Math.Max(20, _numShootSpd.Value * 2));
            _numScale.Value = Clamp(_numScale, Math.Max(2.0m, _numScale.Value * 1.5m));
            _chkHeldAuto.Checked = true;
            ApplyHeldItem();
        }

        private static decimal Clamp(NumericUpDown n, decimal v) => Math.Max(n.Minimum, Math.Min(n.Maximum, v));

        public void SyncFromConfig()
        {
            var cfg = ModConfig.Instance;
            _chkMelee.Checked = cfg.RapidFireMelee;
            _chkRanged.Checked = cfg.RapidFireRanged;
            _chkMagic.Checked = cfg.RapidFireMagic;
            _chkThrow.Checked = cfg.RapidFireThrowables;
            _chkTools.Checked = cfg.RapidFireTools;
            _chkBlocks.Checked = cfg.RapidFireBlocks;
            _chkAutoReuse.Checked = cfg.ForceAutoReuseAll;
            _chkInfAmmo.Checked = cfg.InfiniteAmmo;
            _chkInfThrow.Checked = cfg.InfiniteThrowables;
            _chkInfBlocks.Checked = cfg.InfiniteBlocks;
            _chkMultishot.Checked = cfg.ProjectileMultishotEnabled;
            _chkHoming.Checked = cfg.ProjectileUniversalHoming;
            _chkWallPierce.Checked = cfg.ProjectileNoTileCollide;
            _chkInfPierce.Checked = cfg.ProjectileInfinitePierce;
            _barMultiCount.Value = cfg.ProjectileMultishotCount;
            _barMultiSpread.Value = (int)cfg.ProjectileMultishotSpreadDeg;
            _barHomingRange.Value = (int)cfg.ProjectileHomingRange;
            _barHomingTurn.Value = (int)(cfg.ProjectileHomingTurn * 100f);
            _barProjSpeed.Value = (int)(cfg.ProjectileSpeedMultiplier * 10f);
        }
    }
}

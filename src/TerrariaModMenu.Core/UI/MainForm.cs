using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;
using TerrariaModMenu.Core.Patches;
using TerrariaModMenu.Core.UI.Tabs;

namespace TerrariaModMenu.Core.UI
{
    internal sealed class MainForm : Form
    {
        private const int VK_INSERT = 0x2D;
        private const int VK_F3 = 0x72;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly Timer _telemetryTimer;
        private readonly Label _lblStateBadge;
        private readonly Label _lblHooksBadge;
        private readonly Label _lblPlayerInfo;
        private readonly Label _lblHeldInfo;

        private readonly SidebarNav _sidebar;
        private readonly Panel _sectionHeader;
        private readonly Panel _sectionAccentBar;
        private readonly Label _lblSectionTitle;
        private readonly Label _lblSectionSubtitle;
        private readonly Label _lblSectionCategory;
        private readonly Panel _contentContainer;

        private readonly SurvivalTab _survivalTab;
        private readonly CombatTab _combatTab;
        private readonly CraftingTab _craftingTab;
        private readonly PotionsTab _potionsTab;
        private readonly SpawnerWorldTab _spawnerTab;
        private readonly MapTab _mapTab;
        private readonly SettingsTab _settingsTab;

        private Control? _activeBody;
        private bool _prevInsertDown;
        private bool _prevF3Down;

        public MainForm()
        {
            bool safeMode = ModRuntime.CurrentMode == LaunchMode.Safe;

            Text = safeMode ? "TerraMMod [x86 - Safe Mode]" : "TerraMMod [x86 - Full Mode]";
            ClientSize = new Size(752, 608);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(36, 36);
            TopMost = true;
            BackColor = DarkTheme.BgWindow;
            ForeColor = DarkTheme.TextPrimary;
            Font = DarkTheme.FontNormal;
            Icon? appIcon = SplashForm.LoadEmbeddedIcon();
            if (appIcon != null)
            {
                Icon = appIcon;
            }

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = DarkTheme.BgPanel,
                Padding = new Padding(12, 8, 12, 6)
            };
            header.Paint += (_, e) =>
            {
                using var pen = new Pen(DarkTheme.Border, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var picEmblem = new PictureBox
            {
                Location = new Point(10, 6),
                Size = new Size(36, 36),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = SplashForm.LoadEmbeddedLogo()
            };

            var lblTitle = new Label
            {
                Text = "TERRARIA MOD MENU",
                Font = DarkTheme.FontTitle,
                ForeColor = DarkTheme.TextPrimary,
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(52, 6)
            };

            var lblCredit = new Label
            {
                Text = safeMode ? "by RoyBruna  •  SAFE MODE" : "by RoyBruna  •  FULL MODE",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = safeMode ? DarkTheme.AccentCyan : Color.FromArgb(220, 58, 58),
                AutoSize = false,
                Size = new Size(280, 17),
                UseMnemonic = false,
                Location = new Point(53, 27)
            };

            _lblHooksBadge = new Label
            {
                Text = safeMode ? "SAFE: 0 HOOKS" : $"HOOKS: {HarmonyManager.Installed.Count} OK",
                Font = DarkTheme.FontMono,
                ForeColor = safeMode ? DarkTheme.AccentCyan : (HarmonyManager.Failed.Count == 0 ? DarkTheme.AccentCyan : DarkTheme.AccentAmber),
                AutoSize = false,
                Size = new Size(134, 18),
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false,
                Location = new Point(464, 10)
            };

            _lblStateBadge = new Label
            {
                Text = "MAIN MENU",
                Font = DarkTheme.FontBold,
                ForeColor = DarkTheme.AccentAmber,
                AutoSize = false,
                Size = new Size(138, 18),
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false,
                Location = new Point(602, 9)
            };

            _lblPlayerInfo = new Label
            {
                Text = "Player: - | World: - | HP: -/- | MP: -/- | DEF: -",
                Font = DarkTheme.FontMono,
                ForeColor = DarkTheme.TextPrimary,
                AutoSize = false,
                UseMnemonic = false,
                Size = new Size(728, 18),
                Location = new Point(12, 48)
            };

            _lblHeldInfo = new Label
            {
                Text = "Held: - | [INSERT] Hide/Show",
                Font = DarkTheme.FontMono,
                ForeColor = DarkTheme.TextSecondary,
                AutoSize = false,
                UseMnemonic = false,
                Size = new Size(728, 18),
                Location = new Point(12, 66)
            };

            header.Controls.AddRange(new Control[] { picEmblem, lblTitle, lblCredit, _lblHooksBadge, _lblStateBadge, _lblPlayerInfo, _lblHeldInfo });
            lblCredit.BringToFront();

            _sidebar = new SidebarNav();
            _sidebar.SectionSelected += OnSectionSelected;

            var rightShell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BgWindow
            };

            _sectionHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(22, 25, 34)
            };
            _sectionHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(DarkTheme.Border, 1);
                e.Graphics.DrawLine(pen, 0, _sectionHeader.Height - 1, _sectionHeader.Width, _sectionHeader.Height - 1);
            };

            _sectionAccentBar = new Panel
            {
                Location = new Point(12, 9),
                Size = new Size(4, 24),
                BackColor = DarkTheme.AccentCyan
            };

            _lblSectionTitle = new Label
            {
                Text = "Survival",
                Location = new Point(24, 5),
                Size = new Size(410, 18),
                ForeColor = DarkTheme.TextPrimary,
                Font = DarkTheme.FontBold,
                UseMnemonic = false
            };

            _lblSectionSubtitle = new Label
            {
                Text = "Player vitals, mobility, NoClip and core stat overrides",
                Location = new Point(24, 22),
                Size = new Size(450, 17),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontTiny,
                UseMnemonic = false
            };

            _lblSectionCategory = new Label
            {
                Text = "PLAYER",
                Location = new Point(468, 12),
                Size = new Size(100, 18),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = DarkTheme.TextMuted,
                Font = DarkTheme.FontCategory,
                UseMnemonic = false
            };

            _sectionHeader.Controls.AddRange(new Control[]
            {
                _sectionAccentBar, _lblSectionTitle, _lblSectionSubtitle, _lblSectionCategory
            });

            _contentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BgWindow
            };

            rightShell.Controls.Add(_contentContainer);
            rightShell.Controls.Add(_sectionHeader);

            _survivalTab = new SurvivalTab();
            _combatTab = new CombatTab();
            _craftingTab = new CraftingTab();
            _potionsTab = new PotionsTab();
            _spawnerTab = new SpawnerWorldTab();
            _mapTab = new MapTab();
            _settingsTab = new SettingsTab(this, ResetAllDefaults, Close);

            if (safeMode)
            {
                ApplySafeModeLock(_survivalTab);
                ApplySafeModeLock(_combatTab);
                ApplySafeModeLock(_craftingTab);
                ApplySafeModeLock(_potionsTab);
                ApplySafeModeLock(_spawnerTab);
                ApplySafeModeLock(_mapTab);
            }

            RegisterSection(new SidebarSection(
                "survival", "Player", "Survival",
                "God mode, flight, NoClip and player stat multipliers",
                DarkTheme.AccentGreen, _survivalTab, null, safeMode));

            RegisterSection(new SidebarSection(
                "potions", "Player", "Potions & Buffs",
                "Infinite duration, debuff purge, potion scaling and buff injector",
                DarkTheme.AccentPurple, _potionsTab, () => _potionsTab.EnsureCatalogLoaded(), safeMode));

            RegisterSection(new SidebarSection(
                "combat", "Combat", "Combat & Proj.",
                "Rapid fire, infinite ammo, weapon editor and projectile madness",
                DarkTheme.AccentRed, _combatTab, null, safeMode));

            RegisterSection(new SidebarSection(
                "map", "World", "Interactive Map",
                "Full revealed world canvas, pin teleport, zoom, radar and waypoints",
                DarkTheme.AccentCyan, _mapTab, () => _mapTab.OnTabSelected(), safeMode));

            RegisterSection(new SidebarSection(
                "spawner", "World", "Spawner & World",
                "Item spawner, fullbright, vision toggles, time and spawn rate",
                DarkTheme.AccentBlue, _spawnerTab, () => _spawnerTab.EnsureCatalogLoaded(), safeMode));

            RegisterSection(new SidebarSection(
                "crafting", "Tools", "Crafting",
                "Station bypass, free crafting and full recipe unlock",
                DarkTheme.AccentAmber, _craftingTab, null, safeMode));

            RegisterSection(new SidebarSection(
                "settings", "System", "Settings & Info",
                "Window controls, credits, crash diagnostics and active Harmony hooks",
                DarkTheme.AccentBlue, _settingsTab, () => _settingsTab.RefreshDiagnostics(), false));

            Controls.Add(rightShell);
            Controls.Add(_sidebar);
            Controls.Add(header);

            _sidebar.SelectSection(safeMode ? "settings" : "survival");

            _telemetryTimer = new Timer { Interval = 100 };
            _telemetryTimer.Tick += (_, _) => OnTimerTick();
            _telemetryTimer.Start();
        }

        private void RegisterSection(SidebarSection section)
        {
            section.Body.Visible = false;
            section.Body.Dock = DockStyle.Fill;
            _contentContainer.Controls.Add(section.Body);
            _sidebar.AddSection(section);
        }

        private void OnSectionSelected(SidebarSection section)
        {
            try
            {
                if (_activeBody != null && !ReferenceEquals(_activeBody, section.Body))
                {
                    _activeBody.Visible = false;
                }

                _activeBody = section.Body;
                _activeBody.Visible = true;
                _activeBody.BringToFront();

                _sectionAccentBar.BackColor = section.Accent;
                _lblSectionTitle.Text = section.Title;
                _lblSectionSubtitle.Text = section.Subtitle;
                _lblSectionCategory.Text = section.Category.ToUpperInvariant();
                _lblSectionCategory.ForeColor = section.Accent;

                section.OnSelected?.Invoke();
            }
            catch (Exception ex)
            {
                ModLogger.Error("MainForm.OnSectionSelected", ex);
            }
        }

        private static void ApplySafeModeLock(Control tabBody)
        {
            foreach (Control c in tabBody.Controls)
            {
                c.Enabled = false;
                c.Top += 30;
            }

            var banner = new Label
            {
                Text = "SAFE MODE (Multiplayer / Read-Only) - Gameplay modifications disabled.",
                Location = new Point(8, 6),
                Size = new Size(538, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(24, 54, 72),
                ForeColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontBold,
                UseMnemonic = false
            };
            tabBody.Controls.Add(banner);
            banner.BringToFront();
        }

        private void ResetAllDefaults()
        {
            ModConfig.Instance.ResetAll();
            _survivalTab.SyncFromConfig();
            _combatTab.SyncFromConfig();
            _craftingTab.SyncFromConfig();
            _potionsTab.SyncFromConfig();
            _spawnerTab.SyncFromConfig();
            if (ModRuntime.IsFullModeActive)
            {
                GameActions.EnqueueRefreshRecipes();
            }
        }

        private void OnTimerTick()
        {
            try
            {
                PollHotkeys();

                if (!Visible)
                {
                    return;
                }

                if (ModRuntime.CurrentMode == LaunchMode.Full)
                {
                    _lblHooksBadge.Text = $"HOOKS: {HarmonyManager.Installed.Count} OK";
                }

                GameSnapshot snap = GameState.CaptureSnapshot();
                if (snap.InWorld)
                {
                    if (snap.NetMode == 0)
                    {
                        _lblStateBadge.Text = "IN WORLD (SP)";
                        _lblStateBadge.ForeColor = DarkTheme.AccentGreen;
                    }
                    else if (ModRuntime.CurrentMode == LaunchMode.Safe)
                    {
                        _lblStateBadge.Text = "MULTIPLAYER (SAFE)";
                        _lblStateBadge.ForeColor = DarkTheme.AccentCyan;
                    }
                    else
                    {
                        _lblStateBadge.Text = "MP GUARD (PAUSED)";
                        _lblStateBadge.ForeColor = DarkTheme.AccentAmber;
                    }

                    _lblPlayerInfo.Text =
                        $"{snap.PlayerName} @ {snap.WorldName} | HP {snap.Life}/{snap.LifeMax} | MP {snap.Mana}/{snap.ManaMax} | DEF {snap.Defense}";
                    _lblHeldInfo.Text = snap.HeldItem.Valid
                        ? $"Held: {snap.HeldItem.Name} [{snap.HeldItem.Category}] DMG:{snap.HeldItem.Damage} UT:{snap.HeldItem.UseTime} | Buffs:{snap.ActiveBuffCount}"
                        : $"Held: (empty) | Buffs:{snap.ActiveBuffCount} | [INSERT] Hide/Show";
                }
                else
                {
                    _lblStateBadge.Text = "MAIN MENU";
                    _lblStateBadge.ForeColor = DarkTheme.AccentAmber;
                    _lblPlayerInfo.Text = "Waiting for world load...";
                    _lblHeldInfo.Text = "Held: - | [INSERT] Hide/Show";
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("MainForm.OnTimerTick", ex);
            }
        }

        private void PollHotkeys()
        {
            bool insertDown = (GetAsyncKeyState(VK_INSERT) & 0x8000) != 0;
            if (insertDown && !_prevInsertDown)
            {
                Visible = !Visible;
                if (Visible)
                {
                    BringToFront();
                    Activate();
                }
            }
            _prevInsertDown = insertDown;

            bool f3Down = (GetAsyncKeyState(VK_F3) & 0x8000) != 0;
            if (f3Down && !_prevF3Down && ModRuntime.IsFullModeActive && ModConfig.Instance.HotkeyTeleportCursor)
            {
                GameActions.EnqueueTeleportToCursor();
            }
            _prevF3Down = f3Down;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _telemetryTimer.Stop();
                _telemetryTimer.Dispose();
                ModConfig.Instance.ResetAll();
                if (ModRuntime.IsFullModeActive)
                {
                    GameActions.EnqueueRefreshRecipes();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("MainForm.OnFormClosing", ex);
            }

            base.OnFormClosing(e);
        }
    }
}

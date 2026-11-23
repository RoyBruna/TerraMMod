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

        private readonly SurvivalTab _survivalTab;
        private readonly CombatTab _combatTab;
        private readonly CraftingTab _craftingTab;
        private readonly PotionsTab _potionsTab;
        private readonly SpawnerWorldTab _spawnerTab;
        private readonly SettingsTab _settingsTab;

        private bool _prevInsertDown;
        private bool _prevF3Down;

        public MainForm()
        {
            bool safeMode = ModRuntime.CurrentMode == LaunchMode.Safe;

            Text = safeMode ? "Terraria Mod Menu [x86 - Safe Mode]" : "Terraria Mod Menu [x86 - Full Mode]";
            ClientSize = new Size(572, 612);
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
                Padding = new Padding(12, 8, 12, 8)
            };

            var picEmblem = new PictureBox
            {
                Location = new Point(10, 7),
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
                Location = new Point(52, 6)
            };

            var lblCredit = new Label
            {
                Text = safeMode ? "by RoyBruna  •  SAFE MODE" : "by RoyBruna  •  FULL MODE",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = safeMode ? DarkTheme.AccentCyan : Color.FromArgb(220, 58, 58),
                AutoSize = true,
                Location = new Point(53, 27)
            };

            _lblHooksBadge = new Label
            {
                Text = safeMode ? "SAFE: 0 HOOKS" : $"HOOKS: {HarmonyManager.Installed.Count} OK",
                Font = DarkTheme.FontMono,
                ForeColor = safeMode ? DarkTheme.AccentCyan : (HarmonyManager.Failed.Count == 0 ? DarkTheme.AccentCyan : DarkTheme.AccentAmber),
                AutoSize = true,
                Location = new Point(320, 10)
            };

            _lblStateBadge = new Label
            {
                Text = "MAIN MENU",
                Font = DarkTheme.FontBold,
                ForeColor = DarkTheme.AccentAmber,
                AutoSize = true,
                Location = new Point(448, 10)
            };

            _lblPlayerInfo = new Label
            {
                Text = "Player: - | World: - | HP: -/- | MP: -/- | DEF: -",
                Font = DarkTheme.FontMono,
                ForeColor = DarkTheme.TextPrimary,
                AutoSize = false,
                Size = new Size(548, 18),
                Location = new Point(12, 48)
            };

            _lblHeldInfo = new Label
            {
                Text = "Held: - | [INSERT] Hide/Show",
                Font = DarkTheme.FontMono,
                ForeColor = DarkTheme.TextSecondary,
                AutoSize = false,
                Size = new Size(548, 18),
                Location = new Point(12, 66)
            };

            header.Controls.AddRange(new Control[] { picEmblem, lblTitle, lblCredit, _lblHooksBadge, _lblStateBadge, _lblPlayerInfo, _lblHeldInfo });
            lblCredit.BringToFront();

            var tabHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BgWindow,
                Padding = new Padding(4)
            };

            var tabs = DarkTheme.CreateDarkTabs();

            _survivalTab = new SurvivalTab();
            _combatTab = new CombatTab();
            _craftingTab = new CraftingTab();
            _potionsTab = new PotionsTab();
            _spawnerTab = new SpawnerWorldTab();
            _settingsTab = new SettingsTab(this, ResetAllDefaults, Close);

            if (safeMode)
            {
                ApplySafeModeLock(_survivalTab);
                ApplySafeModeLock(_combatTab);
                ApplySafeModeLock(_craftingTab);
                ApplySafeModeLock(_potionsTab);
                ApplySafeModeLock(_spawnerTab);
            }

            tabs.TabPages.Add(WrapTab("Survival", _survivalTab));
            tabs.TabPages.Add(WrapTab("Combat", _combatTab));
            tabs.TabPages.Add(WrapTab("Crafting", _craftingTab));
            tabs.TabPages.Add(WrapTab("Potions", _potionsTab));
            tabs.TabPages.Add(WrapTab("Spawner", _spawnerTab));
            tabs.TabPages.Add(WrapTab("Settings", _settingsTab));

            if (safeMode)
            {
                tabs.SelectedIndex = 5;
            }

            tabs.SelectedIndexChanged += (_, _) =>
            {
                try
                {
                    if (tabs.SelectedIndex == 3) _potionsTab.EnsureCatalogLoaded();
                    else if (tabs.SelectedIndex == 4) _spawnerTab.EnsureCatalogLoaded();
                    else if (tabs.SelectedIndex == 5) _settingsTab.RefreshDiagnostics();
                }
                catch (Exception ex)
                {
                    ModLogger.Error("MainForm.TabChanged", ex);
                }
            };

            tabHost.Controls.Add(tabs);
            Controls.Add(tabHost);
            Controls.Add(header);

            _telemetryTimer = new Timer { Interval = 100 };
            _telemetryTimer.Tick += (_, _) => OnTimerTick();
            _telemetryTimer.Start();
        }

        private static void ApplySafeModeLock(Control tabBody)
        {
            foreach (Control c in tabBody.Controls)
            {
                c.Enabled = false;
            }

            var banner = new Label
            {
                Text = "SAFE MODE (Multiplayer / Read-Only) - Gameplay modifications disabled. Telemetry & Diagnostics remain active.",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(24, 54, 72),
                ForeColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontBold
            };
            tabBody.Controls.Add(banner);
            banner.BringToFront();
        }

        private static TabPage WrapTab(string title, Control body)
        {
            var page = new TabPage(title)
            {
                BackColor = DarkTheme.BgWindow,
                ForeColor = DarkTheme.TextPrimary
            };
            page.Controls.Add(body);
            return page;
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

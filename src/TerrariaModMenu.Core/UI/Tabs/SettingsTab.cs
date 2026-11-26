using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class SettingsTab : Panel
    {
        private readonly TextBox _txtHooks;
        private readonly Button _btnToggleHooks;

        public SettingsTab(
            Form owner,
            Action onResetAll,
            Action onReloadHooks,
            Action onToggleHooks,
            Action onCloseMenu)
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            bool safeMode = ModRuntime.CurrentMode == LaunchMode.Safe;

            var grpWin = DarkTheme.CreateGroup("Window, Vanilla Reset & Live Hook Controls", 8, 8, 538, 174);
            var chkTop = DarkTheme.CreateCheck("Always on Top", 14, 22, owner.TopMost, v => owner.TopMost = v);
            grpWin.Controls.Add(chkTop);

            DarkTheme.CreateSliderRow(grpWin, "Window Opacity", 14, 46, 240, 50, 100, 100,
                v => $"{v}%", v => owner.Opacity = v / 100.0);

            var lblHotkeys = new Label
            {
                Text = "Hotkeys & Recovery:\n" +
                       "  [INSERT] Toggle Menu Visibility\n" +
                       "  [F3]     Teleport to Cursor\n" +
                       "  Reset / Re-Hook works live (no restart)",
                Location = new Point(266, 20),
                Size = new Size(264, 68),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal,
                UseMnemonic = false
            };
            grpWin.Controls.Add(lblHotkeys);

            var btnReset = DarkTheme.CreateButton("Restore Vanilla", 14, 96, 166, 30, DarkTheme.AccentAmber, onResetAll);
            btnReset.ForeColor = Color.Black;
            btnReset.Enabled = !safeMode;

            var btnReload = DarkTheme.CreateButton("Reload / Re-Hook", 186, 96, 166, 30, DarkTheme.AccentCyan, onReloadHooks);
            btnReload.ForeColor = Color.Black;
            btnReload.Enabled = !safeMode;

            var btnExport = DarkTheme.CreateButton("Export Diagnostic", 358, 96, 166, 30, DarkTheme.AccentBlue, () =>
            {
                string path = CrashReporter.WriteReport("ManualExport", null, false);
                if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show($"Saved to:\n{path}", "TerrariaModMenu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });

            _btnToggleHooks = DarkTheme.CreateButton("Unpatch (Pure Vanilla)", 14, 134, 252, 28, DarkTheme.AccentPurple, onToggleHooks);
            _btnToggleHooks.Enabled = !safeMode;

            var btnClose = DarkTheme.CreateButton("Close & Unpatch", 272, 134, 252, 28, DarkTheme.AccentRed, onCloseMenu);

            grpWin.Controls.AddRange(new Control[] { btnReset, btnReload, btnExport, _btnToggleHooks, btnClose });
            Controls.Add(grpWin);

            var grpCredits = DarkTheme.CreateGroup("Credits", 8, 188, 538, 84);
            var picLogo = new PictureBox
            {
                Location = new Point(14, 22),
                Size = new Size(50, 50),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(4, 4, 6),
                Image = SplashForm.LoadEmbeddedLogo()
            };

            var lblAuthor = new Label
            {
                Text = "Terraria Mod Menu (x86 .NET Framework / Harmony)\nAuthor: RoyBruna  •  Educational Reverse Engineering Project",
                Location = new Point(74, 22),
                Size = new Size(450, 32),
                ForeColor = DarkTheme.TextPrimary,
                Font = DarkTheme.FontNormal,
                UseMnemonic = false
            };

            var lnkGithub = new LinkLabel
            {
                Text = "https://github.com/RoyBruna",
                Location = new Point(74, 56),
                AutoSize = true,
                LinkColor = DarkTheme.AccentCyan,
                ActiveLinkColor = DarkTheme.AccentAmber,
                VisitedLinkColor = DarkTheme.AccentCyan,
                Font = DarkTheme.FontMono
            };
            lnkGithub.LinkClicked += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://github.com/RoyBruna") { UseShellExecute = true });
                }
                catch { }
            };

            grpCredits.Controls.AddRange(new Control[] { picLogo, lblAuthor, lnkGithub });
            Controls.Add(grpCredits);

            var grpDiag = DarkTheme.CreateGroup("Active Harmony Hooks", 8, 278, 538, 190);
            _txtHooks = new TextBox
            {
                Location = new Point(12, 22),
                Size = new Size(514, 156),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.AccentGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Font = DarkTheme.FontMono
            };
            grpDiag.Controls.Add(_txtHooks);
            Controls.Add(grpDiag);

            RefreshDiagnostics();
        }

        public void RefreshDiagnostics()
        {
            if (ModRuntime.CurrentMode == LaunchMode.Full)
            {
                if (ModRuntime.HooksDetached)
                {
                    _btnToggleHooks.Text = "Re-Attach Hooks (Resume)";
                    _btnToggleHooks.BackColor = DarkTheme.AccentGreen;
                }
                else
                {
                    _btnToggleHooks.Text = "Unpatch (Pure Vanilla)";
                    _btnToggleHooks.BackColor = DarkTheme.AccentPurple;
                }
            }

            var sb = new StringBuilder();
            string statusTag = ModRuntime.CurrentMode == LaunchMode.Safe
                ? "SAFE MODE"
                : (ModRuntime.HooksDetached ? "DETACHED - PURE VANILLA" : "ACTIVE");
            sb.AppendLine($"State: {statusTag} | Installed ({HarmonyManager.Installed.Count}):");
            foreach (string h in HarmonyManager.Installed)
            {
                sb.AppendLine($"  [OK]   {h}");
            }
            if (HarmonyManager.Failed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Failed ({HarmonyManager.Failed.Count}):");
                foreach (string f in HarmonyManager.Failed)
                {
                    sb.AppendLine($"  [FAIL] {f}");
                }
            }
            _txtHooks.Text = sb.ToString();
        }
    }
}

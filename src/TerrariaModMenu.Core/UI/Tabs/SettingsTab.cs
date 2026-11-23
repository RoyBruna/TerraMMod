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

        public SettingsTab(Form owner, Action onResetAll, Action onCloseMenu)
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var grpWin = DarkTheme.CreateGroup("Window & Controls", 8, 8, 538, 148);
            var chkTop = DarkTheme.CreateCheck("Always on Top", 14, 22, owner.TopMost, v => owner.TopMost = v);
            grpWin.Controls.Add(chkTop);

            DarkTheme.CreateSliderRow(grpWin, "Window Opacity", 14, 48, 240, 50, 100, 100,
                v => $"{v}%", v => owner.Opacity = v / 100.0);

            var lblHotkeys = new Label
            {
                Text = "Hotkeys:\n" +
                       "  [INSERT] Toggle Menu Visibility\n" +
                       "  [F3]     Teleport to Cursor\n" +
                       "  [Close]  Unpatch all hooks cleanly",
                Location = new Point(270, 20),
                Size = new Size(260, 74),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontNormal
            };
            grpWin.Controls.Add(lblHotkeys);

            var btnReset = DarkTheme.CreateButton("Reset All", 14, 104, 160, 30, DarkTheme.AccentAmber, onResetAll);
            btnReset.ForeColor = Color.Black;
            var btnExport = DarkTheme.CreateButton("Export Diagnostic", 184, 104, 166, 30, DarkTheme.AccentBlue, () =>
            {
                string path = CrashReporter.WriteReport("ManualExport", null, false);
                if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show($"Saved to:\n{path}", "TerrariaModMenu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            var btnClose = DarkTheme.CreateButton("Close & Unpatch", 360, 104, 166, 30, DarkTheme.AccentRed, onCloseMenu);
            grpWin.Controls.AddRange(new Control[] { btnReset, btnExport, btnClose });
            Controls.Add(grpWin);

            var grpCredits = DarkTheme.CreateGroup("Credits", 8, 162, 538, 84);
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
                Font = DarkTheme.FontNormal
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

            var grpDiag = DarkTheme.CreateGroup("Active Harmony Hooks", 8, 252, 538, 212);
            _txtHooks = new TextBox
            {
                Location = new Point(12, 22),
                Size = new Size(514, 178),
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
            var sb = new StringBuilder();
            sb.AppendLine($"Installed ({HarmonyManager.Installed.Count}):");
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

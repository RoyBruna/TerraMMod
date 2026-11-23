using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.UI
{
    internal sealed class SplashForm : Form
    {
        private const string LogoResourceName = "TerrariaModMenu.Core.Assets.logo.jpg";
        private const string IconResourceName = "TerrariaModMenu.Core.Assets.app.ico";

        private readonly Timer _animTimer;
        private readonly Label _lblStatus;
        private readonly Panel _progressFill;
        private readonly Image? _logoImage;

        private int _phase;
        private int _holdTicks;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(340, 404);
            BackColor = Color.FromArgb(4, 4, 6);
            Opacity = 0.0;
            DoubleBuffered = true;

            _logoImage = LoadEmbeddedLogo();
            bool safeMode = ModRuntime.CurrentMode == LaunchMode.Safe;

            var borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(4, 4, 6),
                Padding = new Padding(1)
            };
            borderPanel.Paint += (_, e) =>
            {
                Color borderCol = safeMode ? Color.FromArgb(30, 110, 140) : Color.FromArgb(140, 24, 24);
                using var pen = new Pen(borderCol, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, borderPanel.Width - 1, borderPanel.Height - 1);
            };

            var picLogo = new PictureBox
            {
                Location = new Point(45, 22),
                Size = new Size(250, 250),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(4, 4, 6),
                Image = _logoImage
            };

            var lblTitle = new Label
            {
                Text = "TERRARIA MOD MENU",
                Location = new Point(10, 282),
                Size = new Size(320, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(245, 245, 248),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold)
            };

            var lblAuthor = new Label
            {
                Text = safeMode ? "by RoyBruna  •  Safe Mode (MP)" : "by RoyBruna  •  Full Mode (SP)",
                Location = new Point(10, 310),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = safeMode ? DarkTheme.AccentCyan : Color.FromArgb(220, 58, 58),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var progressTrack = new Panel
            {
                Location = new Point(42, 344),
                Size = new Size(256, 4),
                BackColor = Color.FromArgb(32, 32, 38)
            };

            _progressFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(16, 4),
                BackColor = safeMode ? DarkTheme.AccentCyan : Color.FromArgb(220, 38, 38)
            };
            progressTrack.Controls.Add(_progressFill);

            _lblStatus = new Label
            {
                Text = safeMode ? "Starting read-only telemetry..." : "Syncing hooks on game thread...",
                Location = new Point(10, 358),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = DarkTheme.TextSecondary,
                BackColor = Color.Transparent,
                Font = DarkTheme.FontMono
            };

            borderPanel.Controls.AddRange(new Control[] { picLogo, lblTitle, lblAuthor, progressTrack, _lblStatus });
            Controls.Add(borderPanel);

            _animTimer = new Timer { Interval = 20 };
            _animTimer.Tick += OnAnimationTick;
            _animTimer.Start();
        }

        public static Icon? LoadEmbeddedIcon()
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName);
                return stream != null ? new Icon(stream) : null;
            }
            catch (Exception ex)
            {
                ModLogger.Error("SplashForm.LoadEmbeddedIcon", ex);
                return null;
            }
        }

        public static Image? LoadEmbeddedLogo()
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourceName);
                if (stream == null)
                {
                    return null;
                }

                using var tmp = Image.FromStream(stream);
                return new Bitmap(tmp);
            }
            catch (Exception ex)
            {
                ModLogger.Error("SplashForm.LoadEmbeddedLogo", ex);
                return null;
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            try
            {
                if (_phase == 0)
                {
                    Opacity = Math.Min(1.0, Opacity + 0.08);
                    _progressFill.Width = Math.Min(120, _progressFill.Width + 8);
                    if (Opacity >= 0.99)
                    {
                        Opacity = 1.0;
                        _phase = 1;
                    }
                }
                else if (_phase == 1)
                {
                    _holdTicks++;
                    bool ready = ModRuntime.HooksReady || _holdTicks > 120;
                    if (ready)
                    {
                        _lblStatus.Text = ModRuntime.CurrentMode == LaunchMode.Safe
                            ? "Ready (Safe Mode - 0 hooks)"
                            : $"Ready ({HarmonyManager.Installed.Count} hooks active)";
                        _progressFill.Width = Math.Min(256, _progressFill.Width + 14);
                        if (_holdTicks >= 45 && _progressFill.Width >= 256)
                        {
                            _phase = 2;
                        }
                    }
                    else
                    {
                        _progressFill.Width = Math.Min(210, _progressFill.Width + 2);
                    }
                }
                else
                {
                    Opacity = Math.Max(0.0, Opacity - 0.09);
                    if (Opacity <= 0.02)
                    {
                        _animTimer.Stop();
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("SplashForm.OnAnimationTick", ex);
                _animTimer.Stop();
                Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer.Dispose();
                _logoImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

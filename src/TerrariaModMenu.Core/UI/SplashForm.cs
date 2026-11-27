using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private struct EmberParticle
        {
            public float X;
            public float Y;
            public float SpeedY;
            public float DriftAmp;
            public float DriftPhase;
            public float Size;
            public float Alpha;
        }

        private readonly Timer _animTimer;
        private readonly Image? _logoImage;
        private readonly bool _safeMode;
        private readonly Color _accentPrimary;
        private readonly Color _accentBright;
        private readonly Color _accentDeep;
        private readonly EmberParticle[] _embers;
        private readonly Random _rng = new Random(1337);

        private readonly Font _fontMainTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
        private readonly Font _fontBadge = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private readonly Font _fontStatus = new Font("Consolas", 8.5f, FontStyle.Regular);
        private readonly Font _fontFooter = new Font("Consolas", 7.5f, FontStyle.Regular);

        private float _elapsed;
        private float _progress;
        private float _ringAngleOuter;
        private float _ringAngleInner;
        private float _shimmerPos = -0.3f;
        private float _pulseProgress = -1f;
        private int _phase;
        private float _phaseTimer;
        private string _statusText;

        public SplashForm()
        {
            _safeMode = ModRuntime.CurrentMode == LaunchMode.Safe;
            _accentPrimary = _safeMode ? Color.FromArgb(6, 182, 212) : Color.FromArgb(220, 38, 38);
            _accentBright = _safeMode ? Color.FromArgb(125, 211, 252) : Color.FromArgb(252, 165, 165);
            _accentDeep = _safeMode ? Color.FromArgb(14, 72, 96) : Color.FromArgb(110, 14, 22);
            _statusText = _safeMode ? "Initializing Safe Mode telemetry..." : "Initializing x86 CLR bridge...";

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(420, 446);
            BackColor = Color.FromArgb(5, 5, 8);
            Opacity = 0.0;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            _logoImage = LoadEmbeddedLogo();
            _embers = new EmberParticle[28];
            for (int i = 0; i < _embers.Length; i++)
            {
                _embers[i] = CreateEmber(randomY: true);
            }

            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += OnAnimationTick;
            _animTimer.Start();
        }

        private EmberParticle CreateEmber(bool randomY)
        {
            return new EmberParticle
            {
                X = (float)(_rng.NextDouble() * ClientSize.Width),
                Y = randomY ? (float)(_rng.NextDouble() * ClientSize.Height) : ClientSize.Height + (float)(_rng.NextDouble() * 30.0),
                SpeedY = 18f + (float)(_rng.NextDouble() * 38f),
                DriftAmp = 6f + (float)(_rng.NextDouble() * 14f),
                DriftPhase = (float)(_rng.NextDouble() * Math.PI * 2.0),
                Size = 1.6f + (float)(_rng.NextDouble() * 2.8f),
                Alpha = 0.25f + (float)(_rng.NextDouble() * 0.65f)
            };
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

        public void SetPreviewState(float elapsed, float progress, float opacity, float pulse)
        {
            _animTimer.Stop();
            _elapsed = elapsed;
            _progress = progress;
            _ringAngleOuter = elapsed * 42f;
            _ringAngleInner = -elapsed * 64f;
            _shimmerPos = 0.55f;
            _pulseProgress = pulse;
            _statusText = _safeMode ? "Safe Mode ready  •  0 hooks" : "24 hooks armed  •  Launching interface";
            Opacity = opacity;
            Invalidate();
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            try
            {
                const float dt = 0.016f;
                _elapsed += dt;
                _phaseTimer += dt;
                _ringAngleOuter = (_ringAngleOuter + 40f * dt) % 360f;
                _ringAngleInner = (_ringAngleInner - 62f * dt) % 360f;
                _shimmerPos += 1.35f * dt;
                if (_shimmerPos > 1.4f)
                {
                    _shimmerPos = -0.35f;
                }

                for (int i = 0; i < _embers.Length; i++)
                {
                    _embers[i].Y -= _embers[i].SpeedY * dt;
                    if (_embers[i].Y < -10f)
                    {
                        _embers[i] = CreateEmber(randomY: false);
                    }
                }

                if (_pulseProgress >= 0f && _pulseProgress < 1f)
                {
                    _pulseProgress = Math.Min(1f, _pulseProgress + 2.4f * dt);
                }

                if (_phase == 0)
                {
                    float t = Math.Min(1f, _phaseTimer / 0.45f);
                    float eased = 1f - (float)Math.Pow(1f - t, 3);
                    Opacity = eased;
                    _progress = Math.Max(_progress, eased * 0.42f);
                    if (_phaseTimer > 0.2f && !_safeMode)
                    {
                        _statusText = "Syncing Harmony hooks on game thread...";
                    }

                    if (t >= 1f)
                    {
                        Opacity = 1.0;
                        _phase = 1;
                        _phaseTimer = 0f;
                    }
                }
                else if (_phase == 1)
                {
                    bool ready = ModRuntime.HooksReady || _phaseTimer > 2.2f;
                    if (ready)
                    {
                        _progress = Math.Min(1f, _progress + 1.55f * dt);
                        _statusText = _safeMode
                            ? "Safe Mode ready  •  0 hooks"
                            : $"{HarmonyManager.Installed.Count} hooks armed  •  Launching interface";

                        if (_progress >= 1f && _pulseProgress < 0f)
                        {
                            _pulseProgress = 0f;
                        }

                        if (_progress >= 1f && _phaseTimer >= 1.05f && _pulseProgress >= 0.85f)
                        {
                            _phase = 2;
                            _phaseTimer = 0f;
                        }
                    }
                    else
                    {
                        _progress = Math.Min(0.84f, _progress + 0.35f * dt);
                    }
                }
                else
                {
                    float t = Math.Min(1f, _phaseTimer / 0.32f);
                    Opacity = Math.Max(0.0, 1.0 - t * t);
                    if (t >= 1f)
                    {
                        _animTimer.Stop();
                        Close();
                        return;
                    }
                }

                Invalidate();
            }
            catch (Exception ex)
            {
                ModLogger.Error("SplashForm.OnAnimationTick", ex);
                _animTimer.Stop();
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            var fullRect = new Rectangle(0, 0, w, h);

            Color bgTop = _safeMode ? Color.FromArgb(6, 10, 16) : Color.FromArgb(9, 5, 8);
            Color bgBot = Color.FromArgb(4, 4, 7);
            using (var bgBrush = new LinearGradientBrush(fullRect, bgTop, bgBot, 90f))
            {
                g.FillRectangle(bgBrush, fullRect);
            }

            float cx = w * 0.5f;
            float cy = 166f;
            float breath = 0.5f + 0.5f * (float)Math.Sin(_elapsed * 3.4f);

            float auraRadius = 158f + breath * 14f;
            using (var auraPath = new GraphicsPath())
            {
                auraPath.AddEllipse(cx - auraRadius, cy - auraRadius, auraRadius * 2f, auraRadius * 2f);
                using var auraBrush = new PathGradientBrush(auraPath)
                {
                    CenterColor = Color.FromArgb((int)(68 + breath * 36), _accentPrimary),
                    SurroundColors = new[] { Color.FromArgb(0, _accentDeep) }
                };
                g.FillEllipse(auraBrush, cx - auraRadius, cy - auraRadius, auraRadius * 2f, auraRadius * 2f);
            }

            for (int i = 0; i < _embers.Length; i++)
            {
                EmberParticle em = _embers[i];
                float ex = em.X + (float)Math.Sin(_elapsed * 2.1f + em.DriftPhase) * em.DriftAmp;
                float edgeFade = Math.Min(1f, Math.Min(em.Y / 60f, (h - em.Y) / 50f));
                int a = (int)Math.Max(0f, Math.Min(255f, em.Alpha * Math.Max(0f, edgeFade) * 255f));
                if (a <= 4)
                {
                    continue;
                }

                float glowSize = em.Size * 2.6f;
                using var glowBrush = new SolidBrush(Color.FromArgb(a / 3, _accentPrimary));
                g.FillEllipse(glowBrush, ex - glowSize * 0.5f, em.Y - glowSize * 0.5f, glowSize, glowSize);

                using var coreBrush = new SolidBrush(Color.FromArgb(a, _accentBright));
                g.FillEllipse(coreBrush, ex - em.Size * 0.5f, em.Y - em.Size * 0.5f, em.Size, em.Size);
            }

            float introScale = _phase == 0
                ? 0.82f + 0.18f * (1f - (float)Math.Pow(1f - Math.Min(1f, _phaseTimer / 0.45f), 3))
                : (_phase == 2 ? 1f + 0.06f * Math.Min(1f, _phaseTimer / 0.32f) : 1f);

            float moonRadius = 96f * introScale;

            if (_pulseProgress >= 0f && _pulseProgress < 1f)
            {
                float pr = moonRadius + _pulseProgress * 82f;
                int pa = (int)((1f - _pulseProgress) * 210f);
                using var pulsePen = new Pen(Color.FromArgb(pa, _accentBright), 2.5f * (1f - _pulseProgress) + 0.5f);
                g.DrawEllipse(pulsePen, cx - pr, cy - pr, pr * 2f, pr * 2f);
            }

            float outerR = moonRadius + 17f;
            using (var outerPen = new Pen(Color.FromArgb(175, _accentPrimary), 2f))
            {
                outerPen.StartCap = LineCap.Round;
                outerPen.EndCap = LineCap.Round;
                for (int k = 0; k < 3; k++)
                {
                    g.DrawArc(outerPen, cx - outerR, cy - outerR, outerR * 2f, outerR * 2f, _ringAngleOuter + k * 120f, 68f);
                }
            }

            float innerR = moonRadius + 9f;
            using (var innerPen = new Pen(Color.FromArgb(115, _accentBright), 1.2f))
            {
                innerPen.DashStyle = DashStyle.Dash;
                g.DrawArc(innerPen, cx - innerR, cy - innerR, innerR * 2f, innerR * 2f, _ringAngleInner, 150f);
                g.DrawArc(innerPen, cx - innerR, cy - innerR, innerR * 2f, innerR * 2f, _ringAngleInner + 180f, 150f);
            }

            if (_logoImage != null)
            {
                GraphicsState saved = g.Save();
                using (var clipPath = new GraphicsPath())
                {
                    clipPath.AddEllipse(cx - moonRadius, cy - moonRadius, moonRadius * 2f, moonRadius * 2f);
                    g.SetClip(clipPath);
                    float imgScale = moonRadius * 2.16f;
                    g.DrawImage(_logoImage, cx - imgScale * 0.5f, cy - imgScale * 0.5f, imgScale, imgScale);
                }
                g.Restore(saved);
            }

            using (var rimPen = new Pen(Color.FromArgb((int)(185 + breath * 55), _accentPrimary), 2.2f))
            {
                g.DrawEllipse(rimPen, cx - moonRadius, cy - moonRadius, moonRadius * 2f, moonRadius * 2f);
            }

            var lineRect = new Rectangle(34, 290, w - 68, 2);
            using (var lineBrush = new LinearGradientBrush(lineRect, Color.Transparent, Color.Transparent, 0f))
            {
                lineBrush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.Transparent, Color.FromArgb(190, _accentPrimary), Color.Transparent },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                g.FillRectangle(lineBrush, lineRect);
            }

            var titleRect = new Rectangle(12, 300, w - 24, 30);
            TextRenderer.DrawText(
                g,
                "T E R R A M M O D",
                _fontMainTitle,
                new Rectangle(titleRect.X + 1, titleRect.Y + 2, titleRect.Width, titleRect.Height),
                Color.FromArgb(120, _accentPrimary),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(
                g,
                "T E R R A M M O D",
                _fontMainTitle,
                titleRect,
                Color.FromArgb(245, 246, 250),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            string badgeText = _safeMode ? "BY ROYBRUNA  •  SAFE MODE (MP)" : "BY ROYBRUNA  •  FULL MODE (SP)";
            var pillRect = new Rectangle((w - 246) / 2, 335, 246, 22);
            using (var pillBg = new SolidBrush(Color.FromArgb(46, _accentPrimary)))
            using (var pillBorder = new Pen(Color.FromArgb(135, _accentPrimary), 1f))
            {
                g.FillRectangle(pillBg, pillRect);
                g.DrawRectangle(pillBorder, pillRect);
            }
            TextRenderer.DrawText(
                g,
                badgeText,
                _fontBadge,
                pillRect,
                _accentBright,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int barX = 48;
            int barY = 374;
            int barW = w - 96;
            int barH = 7;
            var trackRect = new Rectangle(barX, barY, barW, barH);
            using (var trackBg = new SolidBrush(Color.FromArgb(22, 24, 32)))
            using (var trackBorder = new Pen(Color.FromArgb(52, 56, 70), 1f))
            {
                g.FillRectangle(trackBg, trackRect);
                g.DrawRectangle(trackBorder, trackRect);
            }

            int fillW = Math.Max(4, (int)(barW * Math.Max(0f, Math.Min(1f, _progress))));
            var fillRect = new Rectangle(barX, barY, fillW, barH);
            using (var fillBrush = new LinearGradientBrush(fillRect, _accentDeep, _accentPrimary, 0f))
            {
                g.FillRectangle(fillBrush, fillRect);
            }

            float shimmerCenter = barX + fillW * _shimmerPos;
            var shimmerRect = new Rectangle((int)(shimmerCenter - 26), barY, 52, barH);
            shimmerRect.Intersect(fillRect);
            if (shimmerRect.Width > 2)
            {
                using var shimmerBrush = new LinearGradientBrush(shimmerRect, Color.Transparent, Color.Transparent, 0f);
                shimmerBrush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.Transparent, Color.FromArgb(190, Color.White), Color.Transparent },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                g.FillRectangle(shimmerBrush, shimmerRect);
            }

            using (var tipGlow = new SolidBrush(Color.FromArgb(165, _accentBright)))
            {
                g.FillEllipse(tipGlow, barX + fillW - 4, barY - 2, 10, barH + 4);
            }

            TextRenderer.DrawText(
                g,
                _statusText,
                _fontStatus,
                new Rectangle(12, 389, w - 24, 20),
                DarkTheme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(
                g,
                "TERRARIA 1.4.6.x  •  x86 RUNTIME  •  [INSERT] TOGGLE",
                _fontFooter,
                new Rectangle(12, 415, w - 24, 18),
                DarkTheme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (var framePen = new Pen(Color.FromArgb(72, _accentPrimary), 1f))
            {
                g.DrawRectangle(framePen, 0, 0, w - 1, h - 1);
            }

            DrawCornerBrackets(g, w, h, 18, Color.FromArgb(225, _accentPrimary));
        }

        private static void DrawCornerBrackets(Graphics g, int w, int h, int len, Color color)
        {
            using var pen = new Pen(color, 2f);
            int pad = 8;
            g.DrawLine(pen, pad, pad, pad + len, pad);
            g.DrawLine(pen, pad, pad, pad, pad + len);

            g.DrawLine(pen, w - pad - len, pad, w - pad, pad);
            g.DrawLine(pen, w - pad, pad, w - pad, pad + len);

            g.DrawLine(pen, pad, h - pad, pad + len, h - pad);
            g.DrawLine(pen, pad, h - pad - len, pad, h - pad);

            g.DrawLine(pen, w - pad - len, h - pad, w - pad, h - pad);
            g.DrawLine(pen, w - pad, h - pad - len, w - pad, h - pad);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer.Dispose();
                _logoImage?.Dispose();
                _fontMainTitle.Dispose();
                _fontBadge.Dispose();
                _fontStatus.Dispose();
                _fontFooter.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

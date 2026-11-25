using System;
using System.Drawing;
using System.Windows.Forms;

namespace TerrariaModMenu.Core.UI
{
    internal static class DarkTheme
    {
        public static readonly Color BgWindow = Color.FromArgb(18, 20, 26);
        public static readonly Color BgPanel = Color.FromArgb(26, 29, 38);
        public static readonly Color BgInput = Color.FromArgb(34, 38, 50);
        public static readonly Color Border = Color.FromArgb(52, 58, 74);

        public static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        public static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);
        public static readonly Color AccentGreen = Color.FromArgb(34, 197, 94);
        public static readonly Color AccentAmber = Color.FromArgb(245, 158, 11);
        public static readonly Color AccentRed = Color.FromArgb(220, 38, 38);
        public static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);

        public static readonly Color TextPrimary = Color.FromArgb(240, 244, 248);
        public static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);

        public static readonly Font FontNormal = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FontMono = new Font("Consolas", 8.5f, FontStyle.Regular);

        public static GroupBox CreateGroup(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title.Replace("&", "&&"),
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = BgPanel,
                ForeColor = AccentCyan,
                Font = FontBold
            };
        }

        public static CheckBox CreateCheck(string text, int x, int y, bool initial, Action<bool> onChange, string? tooltip = null, ToolTip? tip = null)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                UseMnemonic = false,
                Checked = initial,
                ForeColor = TextPrimary,
                BackColor = Color.Transparent,
                Font = FontNormal
            };
            chk.CheckedChanged += (_, _) =>
            {
                try { onChange(chk.Checked); }
                catch { }
            };
            if (tip != null && !string.IsNullOrEmpty(tooltip))
            {
                tip.SetToolTip(chk, tooltip);
            }
            return chk;
        }

        public static Button CreateButton(string text, int x, int y, int w, int h, Color bg, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                BackColor = bg,
                ForeColor = TextPrimary,
                Font = FontBold,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (_, _) =>
            {
                try { onClick(); }
                catch { }
            };
            return btn;
        }

        public static (TrackBar Bar, Label ValueLabel) CreateSliderRow(
            Control parent,
            string title,
            int x,
            int y,
            int totalWidth,
            int min,
            int max,
            int initial,
            Func<int, string> format,
            Action<int> onChange)
        {
            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(totalWidth - 78, 17),
                UseMnemonic = false,
                ForeColor = TextPrimary,
                BackColor = Color.Transparent,
                Font = FontNormal
            };
            var lblVal = new Label
            {
                Text = format(initial),
                Location = new Point(x + totalWidth - 76, y),
                Size = new Size(76, 17),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = AccentAmber,
                BackColor = Color.Transparent,
                Font = FontMono
            };
            var bar = new TrackBar
            {
                AutoSize = false,
                Location = new Point(x - 4, y + 18),
                Size = new Size(totalWidth + 6, 22),
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, initial)),
                TickStyle = TickStyle.None,
                BackColor = parent.BackColor
            };
            bar.ValueChanged += (_, _) =>
            {
                try
                {
                    lblVal.Text = format(bar.Value);
                    onChange(bar.Value);
                }
                catch { }
            };

            parent.Controls.Add(bar);
            parent.Controls.Add(lblTitle);
            parent.Controls.Add(lblVal);
            lblTitle.BringToFront();
            lblVal.BringToFront();
            return (bar, lblVal);
        }

        public static NumericUpDown CreateNumeric(int x, int y, int w, decimal min, decimal max, decimal value, int decimals = 0)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(w, 22),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Value = Math.Max(min, Math.Min(max, value)),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontMono
            };
        }

        public static TextBox CreateTextBox(int x, int y, int w, string placeholder = "")
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 23),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontNormal,
                Text = placeholder
            };
        }

        public static TabControl CreateDarkTabs()
        {
            var tc = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(76, 28),
                Padding = new Point(4, 4),
                Font = FontBold
            };

            tc.DrawItem += (_, e) =>
            {
                try
                {
                    TabPage page = tc.TabPages[e.Index];
                    bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                    Rectangle bounds = e.Bounds;

                    using var bgBrush = new SolidBrush(selected ? BgPanel : BgWindow);
                    e.Graphics.FillRectangle(bgBrush, bounds);

                    if (selected)
                    {
                        using var accentBrush = new SolidBrush(AccentCyan);
                        e.Graphics.FillRectangle(accentBrush, bounds.Left, bounds.Bottom - 3, bounds.Width, 3);
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        page.Text,
                        selected ? FontBold : FontNormal,
                        bounds,
                        selected ? TextPrimary : TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                catch { }
            };

            return tc;
        }
    }
}

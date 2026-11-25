using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TerrariaModMenu.Core.UI
{
    internal sealed class SidebarSection
    {
        public string Key { get; }
        public string Category { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public Color Accent { get; }
        public Control Body { get; }
        public Action? OnSelected { get; }
        public bool Locked { get; }

        public SidebarSection(
            string key,
            string category,
            string title,
            string subtitle,
            Color accent,
            Control body,
            Action? onSelected = null,
            bool locked = false)
        {
            Key = key;
            Category = category;
            Title = title;
            Subtitle = subtitle;
            Accent = accent;
            Body = body;
            OnSelected = onSelected;
            Locked = locked;
        }
    }

    internal sealed class SidebarNavItem : Control
    {
        private bool _hovered;
        private bool _selected;

        public SidebarSection Section { get; }

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    Invalidate();
                }
            }
        }

        public SidebarNavItem(SidebarSection section)
        {
            Section = section;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.Selectable,
                true);
            Height = 32;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color bg = _selected
                ? DarkTheme.BgSelected
                : (_hovered ? DarkTheme.BgHover : DarkTheme.BgSidebar);

            using (var bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            if (_selected)
            {
                using var barBrush = new SolidBrush(Section.Accent);
                g.FillRectangle(barBrush, 0, 3, 3, Height - 6);
            }
            else
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var dotBrush = new SolidBrush(Color.FromArgb(_hovered ? 215 : 130, Section.Accent));
                g.FillEllipse(dotBrush, 10, (Height - 6) / 2, 6, 6);
                g.SmoothingMode = SmoothingMode.Default;
            }

            int textLeft = _selected ? 14 : 22;
            int rightReserve = Section.Locked ? 44 : 8;
            var textRect = new Rectangle(textLeft, 0, Math.Max(10, Width - textLeft - rightReserve), Height);

            Color fg = _selected
                ? DarkTheme.TextPrimary
                : (_hovered ? DarkTheme.TextPrimary : DarkTheme.TextSecondary);

            TextRenderer.DrawText(
                g,
                Section.Title,
                _selected ? DarkTheme.FontNavBold : DarkTheme.FontNav,
                textRect,
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (Section.Locked)
            {
                var badgeRect = new Rectangle(Width - 42, (Height - 16) / 2, 34, 16);
                using var badgeBg = new SolidBrush(Color.FromArgb(30, 56, 72));
                g.FillRectangle(badgeBg, badgeRect);
                TextRenderer.DrawText(
                    g,
                    "SAFE",
                    DarkTheme.FontTiny,
                    badgeRect,
                    DarkTheme.AccentCyan,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    internal sealed class SidebarNav : Panel
    {
        private readonly Panel _scrollHost;
        private readonly List<SidebarNavItem> _items = new List<SidebarNavItem>();
        private readonly Dictionary<string, SidebarNavItem> _byKey = new Dictionary<string, SidebarNavItem>(StringComparer.OrdinalIgnoreCase);
        private string _lastCategory = string.Empty;
        private int _nextY = 6;

        public event Action<SidebarSection>? SectionSelected;

        public SidebarNav()
        {
            Dock = DockStyle.Left;
            Width = 172;
            BackColor = DarkTheme.BgSidebar;
            Padding = new Padding(0, 0, 1, 0);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(11, 13, 18),
                Padding = new Padding(10, 6, 8, 6)
            };
            footer.Paint += (_, e) =>
            {
                using var pen = new Pen(DarkTheme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            var lblHotkeys = new Label
            {
                Text = "[INS] Show / Hide\n[F3]  Cursor Teleport",
                Dock = DockStyle.Fill,
                ForeColor = DarkTheme.TextMuted,
                Font = DarkTheme.FontTiny,
                UseMnemonic = false
            };
            footer.Controls.Add(lblHotkeys);

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BgSidebar,
                AutoScroll = true
            };

            Controls.Add(_scrollHost);
            Controls.Add(footer);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(DarkTheme.Border, 1);
            e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }

        public void AddSection(SidebarSection section)
        {
            if (!string.Equals(_lastCategory, section.Category, StringComparison.OrdinalIgnoreCase))
            {
                if (_items.Count > 0)
                {
                    _nextY += 8;
                }

                var lblCat = new Label
                {
                    Text = section.Category.ToUpperInvariant(),
                    Location = new Point(10, _nextY),
                    Size = new Size(Width - 22, 18),
                    ForeColor = DarkTheme.TextMuted,
                    BackColor = Color.Transparent,
                    Font = DarkTheme.FontCategory,
                    UseMnemonic = false,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                _scrollHost.Controls.Add(lblCat);
                _nextY += 20;
                _lastCategory = section.Category;
            }

            var item = new SidebarNavItem(section)
            {
                Location = new Point(0, _nextY),
                Width = Width - 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            item.Click += (_, _) => SelectSection(section.Key);

            _scrollHost.Controls.Add(item);
            _items.Add(item);
            _byKey[section.Key] = item;
            _nextY += item.Height + 1;
        }

        public void SelectSection(string key)
        {
            if (!_byKey.TryGetValue(key, out SidebarNavItem? target))
            {
                return;
            }

            foreach (SidebarNavItem item in _items)
            {
                item.Selected = ReferenceEquals(item, target);
            }

            SectionSelected?.Invoke(target.Section);
        }
    }
}

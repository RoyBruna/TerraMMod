using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Terraria;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.UI.Tabs
{
    internal sealed class MapCanvas : Control
    {
        public Bitmap? MapBitmap { get; set; }
        public MapRenderResult? LastResult { get; set; }
        public bool HasPin { get; set; }
        public int PinTileX { get; set; }
        public int PinTileY { get; set; }
        public bool ShowNpcs { get; set; } = true;
        public bool ShowBosses { get; set; } = true;
        public bool ShowLandmarks { get; set; } = true;

        public MapCanvas()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Cross;
            BackColor = Color.FromArgb(12, 14, 18);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (MapBitmap != null)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(MapBitmap, 0, 0, Width, Height);
            }
            else
            {
                using var bg = new SolidBrush(BackColor);
                g.FillRectangle(bg, ClientRectangle);
                TextRenderer.DrawText(
                    g,
                    "Join a world and click 'Render Full Map' to load the live interactive world map.",
                    DarkTheme.FontNormal,
                    ClientRectangle,
                    DarkTheme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            using (var borderPen = new Pen(DarkTheme.Border, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            MapRenderResult? res = LastResult;
            if (res == null)
            {
                return;
            }

            foreach (MapOverlayPoint pt in res.Markers)
            {
                if (pt.Kind == MapMarkerKind.TownNpc && !ShowNpcs) continue;
                if (pt.Kind == MapMarkerKind.Boss && !ShowBosses) continue;
                if ((pt.Kind == MapMarkerKind.WorldSpawn || pt.Kind == MapMarkerKind.BedSpawn ||
                     pt.Kind == MapMarkerKind.Dungeon || pt.Kind == MapMarkerKind.LastDeath) && !ShowLandmarks)
                {
                    continue;
                }

                if (!TryTileToCanvas(res, pt.TileX, pt.TileY, out float cx, out float cy))
                {
                    continue;
                }

                DrawMarker(g, pt.Kind, cx, cy);
            }

            if (HasPin && TryTileToCanvas(res, PinTileX, PinTileY, out float px, out float py))
            {
                using var outerPen = new Pen(Color.FromArgb(220, 38, 38), 2f);
                using var innerPen = new Pen(Color.FromArgb(250, 204, 21), 1.5f);
                using var centerBrush = new SolidBrush(Color.FromArgb(250, 204, 21));

                g.DrawEllipse(outerPen, px - 8, py - 8, 16, 16);
                g.DrawEllipse(innerPen, px - 5, py - 5, 10, 10);
                g.FillEllipse(centerBrush, px - 2.5f, py - 2.5f, 5, 5);
                g.DrawLine(outerPen, px - 12, py, px - 4, py);
                g.DrawLine(outerPen, px + 4, py, px + 12, py);
                g.DrawLine(outerPen, px, py - 12, px, py - 4);
                g.DrawLine(outerPen, px, py + 4, px, py + 12);
            }
        }

        private static void DrawMarker(Graphics g, MapMarkerKind kind, float x, float y)
        {
            switch (kind)
            {
                case MapMarkerKind.Player:
                {
                    using var fill = new SolidBrush(Color.FromArgb(34, 197, 94));
                    using var ring = new Pen(Color.White, 1.8f);
                    g.FillEllipse(fill, x - 4.5f, y - 4.5f, 9f, 9f);
                    g.DrawEllipse(ring, x - 4.5f, y - 4.5f, 9f, 9f);
                    break;
                }
                case MapMarkerKind.WorldSpawn:
                {
                    DrawDiamond(g, x, y, 4.5f, Color.FromArgb(6, 182, 212), Color.White);
                    break;
                }
                case MapMarkerKind.BedSpawn:
                {
                    DrawDiamond(g, x, y, 4f, Color.FromArgb(59, 130, 246), Color.White);
                    break;
                }
                case MapMarkerKind.Dungeon:
                {
                    DrawDiamond(g, x, y, 4.5f, Color.FromArgb(168, 85, 247), Color.White);
                    break;
                }
                case MapMarkerKind.LastDeath:
                {
                    using var pen = new Pen(Color.FromArgb(239, 68, 68), 2f);
                    g.DrawLine(pen, x - 4, y - 4, x + 4, y + 4);
                    g.DrawLine(pen, x + 4, y - 4, x - 4, y + 4);
                    break;
                }
                case MapMarkerKind.TownNpc:
                {
                    using var fill = new SolidBrush(Color.FromArgb(250, 204, 21));
                    using var ring = new Pen(Color.Black, 1f);
                    g.FillEllipse(fill, x - 2.5f, y - 2.5f, 5f, 5f);
                    g.DrawEllipse(ring, x - 2.5f, y - 2.5f, 5f, 5f);
                    break;
                }
                case MapMarkerKind.Boss:
                {
                    using var fill = new SolidBrush(Color.FromArgb(220, 38, 38));
                    using var ring = new Pen(Color.FromArgb(254, 240, 138), 1.8f);
                    g.FillEllipse(fill, x - 6f, y - 6f, 12f, 12f);
                    g.DrawEllipse(ring, x - 6f, y - 6f, 12f, 12f);
                    break;
                }
            }
        }

        private static void DrawDiamond(Graphics g, float x, float y, float r, Color fillCol, Color borderCol)
        {
            PointF[] pts =
            {
                new PointF(x, y - r),
                new PointF(x + r, y),
                new PointF(x, y + r),
                new PointF(x - r, y)
            };
            using var fill = new SolidBrush(fillCol);
            using var pen = new Pen(borderCol, 1.2f);
            g.FillPolygon(fill, pts);
            g.DrawPolygon(pen, pts);
        }

        public bool TryTileToCanvas(MapRenderResult res, int tileX, int tileY, out float cx, out float cy)
        {
            cx = 0;
            cy = 0;
            if (tileX < res.ViewMinX || tileX > res.ViewMaxX || tileY < res.ViewMinY || tileY > res.ViewMaxY)
            {
                return false;
            }

            float spanX = Math.Max(1, res.ViewMaxX - res.ViewMinX);
            float spanY = Math.Max(1, res.ViewMaxY - res.ViewMinY);
            cx = ((tileX - res.ViewMinX) / spanX) * Width;
            cy = ((tileY - res.ViewMinY) / spanY) * Height;
            return true;
        }

        public bool TryCanvasToTile(int canvasX, int canvasY, out int tileX, out int tileY)
        {
            tileX = 0;
            tileY = 0;
            MapRenderResult? res = LastResult;
            if (res == null || Width <= 0 || Height <= 0)
            {
                return false;
            }

            float nx = Math.Max(0f, Math.Min(1f, canvasX / (float)Width));
            float ny = Math.Max(0f, Math.Min(1f, canvasY / (float)Height));

            tileX = res.ViewMinX + (int)(nx * (res.ViewMaxX - res.ViewMinX));
            tileY = res.ViewMinY + (int)(ny * (res.ViewMaxY - res.ViewMinY));
            return true;
        }
    }

    internal sealed class WaypointEntry
    {
        public string Name { get; }
        public int TileX { get; }
        public int TileY { get; }

        public WaypointEntry(string name, int tileX, int tileY)
        {
            Name = name;
            TileX = tileX;
            TileY = tileY;
        }

        public override string ToString() => $"{Name} ({TileX}, {TileY})";
    }

    internal sealed class MapTab : Panel
    {
        private readonly MapCanvas _canvas;
        private readonly Label _lblCoords;
        private readonly Label _lblPinStatus;
        private readonly CheckBox _chkInstantTp;
        private readonly CheckBox _chkAutoRefresh;
        private readonly TextBox _txtWaypointName;
        private readonly ComboBox _cmbWaypoints;
        private readonly Timer _refreshTimer;

        private float _zoom = 1.0f;
        private int _viewCenterX;
        private int _viewCenterY;
        private bool _panning;
        private Point _panStartMouse;
        private int _panStartCenterX;
        private int _panStartCenterY;

        public MapTab()
        {
            Dock = DockStyle.Fill;
            BackColor = DarkTheme.BgWindow;
            AutoScroll = true;
            Padding = new Padding(8);

            var grpMap = DarkTheme.CreateGroup("Interactive World Map (L-Click: Pin | R/Dbl-Click: TP | Wheel: Zoom)", 8, 6, 538, 294);

            _canvas = new MapCanvas
            {
                Location = new Point(11, 20),
                Size = new Size(516, 204)
            };

            _canvas.MouseDown += OnCanvasMouseDown;
            _canvas.MouseMove += OnCanvasMouseMove;
            _canvas.MouseUp += OnCanvasMouseUp;
            _canvas.MouseDoubleClick += OnCanvasDoubleClick;
            _canvas.MouseWheel += OnCanvasMouseWheel;
            _canvas.MouseEnter += (_, _) =>
            {
                if (Form.ActiveForm == FindForm())
                {
                    _canvas.Focus();
                }
            };

            _lblCoords = new Label
            {
                Text = "Cursor: (-, -) | Layer: - | Zoom: 1.0x",
                Location = new Point(12, 228),
                Size = new Size(280, 18),
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.FontMono
            };

            _lblPinStatus = new Label
            {
                Text = "Pin: (none selected)",
                Location = new Point(296, 228),
                Size = new Size(230, 18),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = DarkTheme.AccentAmber,
                Font = DarkTheme.FontMono
            };

            var btnTpPin = DarkTheme.CreateButton("Teleport to Pin", 11, 250, 126, 32, DarkTheme.AccentGreen, TeleportToActivePin);
            var btnRender = DarkTheme.CreateButton("Render Full Map", 143, 250, 118, 32, DarkTheme.AccentBlue, RequestMapUpdate);
            var btnSyncIngame = DarkTheme.CreateButton("Reveal In-Game", 267, 250, 134, 32, DarkTheme.AccentAmber, () =>
            {
                GameActions.EnqueueRevealFullMap();
                RequestMapUpdate();
            });
            btnSyncIngame.ForeColor = Color.Black;

            var btnResetView = DarkTheme.CreateButton("1x Full View", 407, 250, 120, 32, DarkTheme.BgInput, () =>
            {
                _zoom = 1.0f;
                _viewCenterX = 0;
                _viewCenterY = 0;
                RequestMapUpdate();
            });

            grpMap.Controls.AddRange(new Control[]
            {
                _canvas, _lblCoords, _lblPinStatus,
                btnTpPin, btnRender, btnSyncIngame, btnResetView
            });
            Controls.Add(grpMap);

            _refreshTimer = new Timer { Interval = 2500, Enabled = true };
            _refreshTimer.Tick += (_, _) =>
            {
                if (Visible)
                {
                    RequestMapUpdate();
                }
            };

            var grpOptions = DarkTheme.CreateGroup("Map Controls & Radar Filters", 8, 304, 538, 72);

            _chkInstantTp = DarkTheme.CreateCheck("Instant TP on Left-Click", 12, 22, false, _ => { });
            _chkAutoRefresh = DarkTheme.CreateCheck("Auto-Refresh (2.5s)", 184, 22, true, v => _refreshTimer.Enabled = v);

            var chkLandmarks = DarkTheme.CreateCheck("Landmarks", 346, 22, true, v => { _canvas.ShowLandmarks = v; _canvas.Invalidate(); });
            var chkNpcs = DarkTheme.CreateCheck("NPCs", 438, 22, true, v => { _canvas.ShowNpcs = v; _canvas.Invalidate(); });
            var chkBosses = DarkTheme.CreateCheck("Bosses", 12, 45, true, v => { _canvas.ShowBosses = v; _canvas.Invalidate(); });

            var btnZ2 = DarkTheme.CreateButton("2x", 184, 43, 42, 22, DarkTheme.BgInput, () => SetZoom(2f));
            var btnZ4 = DarkTheme.CreateButton("4x", 230, 43, 42, 22, DarkTheme.BgInput, () => SetZoom(4f));
            var btnZ8 = DarkTheme.CreateButton("8x", 276, 43, 42, 22, DarkTheme.BgInput, () => SetZoom(8f));
            var btnZ16 = DarkTheme.CreateButton("16x", 322, 43, 46, 22, DarkTheme.BgInput, () => SetZoom(16f));
            var btnFocusMe = DarkTheme.CreateButton("Center on Player", 376, 43, 150, 22, DarkTheme.AccentCyan, CenterOnPlayer);
            btnFocusMe.ForeColor = Color.Black;

            grpOptions.Controls.AddRange(new Control[]
            {
                _chkInstantTp, _chkAutoRefresh, chkLandmarks, chkNpcs, chkBosses,
                btnZ2, btnZ4, btnZ8, btnZ16, btnFocusMe
            });
            Controls.Add(grpOptions);

            var grpDestinations = DarkTheme.CreateGroup("Quick Landmarks & Saved Pin Waypoints", 8, 380, 538, 92);

            var btnSpawn = DarkTheme.CreateButton("Spawn", 12, 22, 68, 26, DarkTheme.BgInput, () => GameActions.EnqueueTeleportLandmark("Spawn"));
            var btnBed = DarkTheme.CreateButton("Bed", 84, 22, 62, 26, DarkTheme.BgInput, () => GameActions.EnqueueTeleportLandmark("Bed"));
            var btnDungeon = DarkTheme.CreateButton("Dungeon", 150, 22, 76, 26, DarkTheme.AccentPurple, () => GameActions.EnqueueTeleportLandmark("Dungeon"));
            var btnDeath = DarkTheme.CreateButton("Last Death", 230, 22, 84, 26, DarkTheme.AccentRed, () => GameActions.EnqueueTeleportLandmark("Death"));
            var btnOceanL = DarkTheme.CreateButton("West Sea", 318, 22, 72, 26, DarkTheme.AccentBlue, () => GameActions.EnqueueTeleportLandmark("LeftOcean"));
            var btnOceanR = DarkTheme.CreateButton("East Sea", 394, 22, 72, 26, DarkTheme.AccentBlue, () => GameActions.EnqueueTeleportLandmark("RightOcean"));
            var btnHell = DarkTheme.CreateButton("Hell", 470, 22, 56, 26, DarkTheme.AccentAmber, () => GameActions.EnqueueTeleportLandmark("Hell"));
            btnHell.ForeColor = Color.Black;

            _txtWaypointName = DarkTheme.CreateTextBox(12, 56, 128, "Pin 1");
            var btnSavePin = DarkTheme.CreateButton("Save Pin", 146, 55, 82, 24, DarkTheme.AccentCyan, SaveCurrentPinWaypoint);
            btnSavePin.ForeColor = Color.Black;

            _cmbWaypoints = new ComboBox
            {
                Location = new Point(234, 55),
                Size = new Size(192, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = DarkTheme.BgInput,
                ForeColor = DarkTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.FontMono
            };

            var btnTpWp = DarkTheme.CreateButton("TP", 432, 55, 46, 24, DarkTheme.AccentGreen, TeleportSelectedWaypoint);
            var btnDelWp = DarkTheme.CreateButton("Del", 482, 55, 44, 24, DarkTheme.BgInput, DeleteSelectedWaypoint);

            grpDestinations.Controls.AddRange(new Control[]
            {
                btnSpawn, btnBed, btnDungeon, btnDeath, btnOceanL, btnOceanR, btnHell,
                _txtWaypointName, btnSavePin, _cmbWaypoints, btnTpWp, btnDelWp
            });
            Controls.Add(grpDestinations);
        }

        public void OnTabSelected()
        {
            RequestMapUpdate();
        }

        private void SetZoom(float z)
        {
            _zoom = z;
            if (_canvas.HasPin)
            {
                _viewCenterX = _canvas.PinTileX;
                _viewCenterY = _canvas.PinTileY;
            }
            RequestMapUpdate();
        }

        private void CenterOnPlayer()
        {
            MapRenderResult? res = _canvas.LastResult;
            if (res != null)
            {
                _viewCenterX = res.PlayerTileX;
                _viewCenterY = res.PlayerTileY;
                RequestMapUpdate();
            }
        }

        public void RequestMapUpdate()
        {
            WorldMapScanner.RequestScan(
                _canvas.Width,
                _canvas.Height,
                _zoom,
                _viewCenterX,
                _viewCenterY,
                result =>
                {
                    if (IsDisposed || !IsHandleCreated)
                    {
                        return;
                    }

                    try
                    {
                        BeginInvoke(new Action(() => ApplyRenderResult(result)));
                    }
                    catch
                    {
                    }
                });
        }

        private void ApplyRenderResult(MapRenderResult result)
        {
            try
            {
                Bitmap? bmp = _canvas.MapBitmap;
                if (bmp == null || bmp.Width != result.Width || bmp.Height != result.Height)
                {
                    bmp?.Dispose();
                    bmp = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppRgb);
                    _canvas.MapBitmap = bmp;
                }

                BitmapData data = bmp.LockBits(
                    new Rectangle(0, 0, result.Width, result.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppRgb);
                try
                {
                    Marshal.Copy(result.Pixels, 0, data.Scan0, result.Pixels.Length);
                }
                finally
                {
                    bmp.UnlockBits(data);
                }

                _canvas.LastResult = result;
                _canvas.Invalidate();
            }
            catch (Exception ex)
            {
                ModLogger.Error("MapTab.ApplyRenderResult", ex);
            }
        }

        private void OnCanvasMouseDown(object? sender, MouseEventArgs e)
        {
            if (!_canvas.TryCanvasToTile(e.X, e.Y, out int tx, out int ty))
            {
                return;
            }

            if (e.Button == MouseButtons.Middle)
            {
                _panning = true;
                _panStartMouse = e.Location;
                MapRenderResult? res = _canvas.LastResult;
                if (res != null)
                {
                    _panStartCenterX = (res.ViewMinX + res.ViewMaxX) / 2;
                    _panStartCenterY = (res.ViewMinY + res.ViewMaxY) / 2;
                }
                return;
            }

            PlacePin(tx, ty);

            if (e.Button == MouseButtons.Right || (e.Button == MouseButtons.Left && _chkInstantTp.Checked))
            {
                TeleportToActivePin();
            }
        }

        private void OnCanvasDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_canvas.TryCanvasToTile(e.X, e.Y, out int tx, out int ty))
            {
                PlacePin(tx, ty);
                TeleportToActivePin();
            }
        }

        private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
        {
            MapRenderResult? res = _canvas.LastResult;
            if (res == null)
            {
                return;
            }

            if (_panning && _zoom > 1.01f)
            {
                int dxPx = e.X - _panStartMouse.X;
                int dyPx = e.Y - _panStartMouse.Y;
                int spanX = res.ViewMaxX - res.ViewMinX;
                int spanY = res.ViewMaxY - res.ViewMinY;
                _viewCenterX = _panStartCenterX - (dxPx * spanX) / Math.Max(1, _canvas.Width);
                _viewCenterY = _panStartCenterY - (dyPx * spanY) / Math.Max(1, _canvas.Height);
                return;
            }

            if (_canvas.TryCanvasToTile(e.X, e.Y, out int tx, out int ty))
            {
                string layer = GetDepthLayerName(ty);
                _lblCoords.Text = $"Cursor: ({tx}, {ty}) | {layer} | {_zoom:0.0}x";
            }
        }

        private void OnCanvasMouseUp(object? sender, MouseEventArgs e)
        {
            if (_panning)
            {
                _panning = false;
                RequestMapUpdate();
            }
        }

        private void OnCanvasMouseWheel(object? sender, MouseEventArgs e)
        {
            if (_canvas.TryCanvasToTile(e.X, e.Y, out int tx, out int ty))
            {
                _viewCenterX = tx;
                _viewCenterY = ty;
            }

            if (e.Delta > 0)
            {
                _zoom = Math.Min(16f, _zoom * 1.5f);
            }
            else if (e.Delta < 0)
            {
                _zoom = Math.Max(1f, _zoom / 1.5f);
                if (_zoom <= 1.05f)
                {
                    _zoom = 1f;
                    _viewCenterX = 0;
                    _viewCenterY = 0;
                }
            }

            RequestMapUpdate();
        }

        private void PlacePin(int tileX, int tileY)
        {
            _canvas.HasPin = true;
            _canvas.PinTileX = tileX;
            _canvas.PinTileY = tileY;
            _lblPinStatus.Text = $"Pin: ({tileX}, {tileY}) [{GetDepthLayerName(tileY)}]";
            _canvas.Invalidate();
        }

        private void TeleportToActivePin()
        {
            if (!_canvas.HasPin || !ModRuntime.IsFullModeActive)
            {
                return;
            }

            GameActions.EnqueueTeleportToTile(_canvas.PinTileX, _canvas.PinTileY, "Map Pin");
        }

        private void SaveCurrentPinWaypoint()
        {
            int tx;
            int ty;
            if (_canvas.HasPin)
            {
                tx = _canvas.PinTileX;
                ty = _canvas.PinTileY;
            }
            else if (_canvas.LastResult != null)
            {
                tx = _canvas.LastResult.PlayerTileX;
                ty = _canvas.LastResult.PlayerTileY;
            }
            else
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(_txtWaypointName.Text)
                ? $"WP {_cmbWaypoints.Items.Count + 1}"
                : _txtWaypointName.Text.Trim();

            var entry = new WaypointEntry(name, tx, ty);
            _cmbWaypoints.Items.Add(entry);
            _cmbWaypoints.SelectedItem = entry;
            _txtWaypointName.Text = $"Pin {_cmbWaypoints.Items.Count + 1}";
        }

        private void TeleportSelectedWaypoint()
        {
            if (_cmbWaypoints.SelectedItem is WaypointEntry wp && ModRuntime.IsFullModeActive)
            {
                PlacePin(wp.TileX, wp.TileY);
                GameActions.EnqueueTeleportToTile(wp.TileX, wp.TileY, wp.Name);
            }
        }

        private void DeleteSelectedWaypoint()
        {
            if (_cmbWaypoints.SelectedIndex >= 0)
            {
                _cmbWaypoints.Items.RemoveAt(_cmbWaypoints.SelectedIndex);
                if (_cmbWaypoints.Items.Count > 0)
                {
                    _cmbWaypoints.SelectedIndex = 0;
                }
            }
        }

        private static string GetDepthLayerName(int tileY)
        {
            if (tileY > Main.maxTilesY - 200) return "Underworld";
            if (tileY > Main.rockLayer) return "Cavern";
            if (tileY > Main.worldSurface) return "Underground";
            if (tileY < Main.worldSurface * 0.35) return "Space";
            return "Surface";
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;

namespace TerrariaModMenu.Core.Core
{
    internal enum MapMarkerKind
    {
        Player = 0,
        WorldSpawn,
        BedSpawn,
        Dungeon,
        LastDeath,
        TownNpc,
        Boss
    }

    internal readonly struct MapOverlayPoint
    {
        public MapMarkerKind Kind { get; }
        public int TileX { get; }
        public int TileY { get; }
        public string Label { get; }

        public MapOverlayPoint(MapMarkerKind kind, int tileX, int tileY, string label)
        {
            Kind = kind;
            TileX = tileX;
            TileY = tileY;
            Label = label;
        }
    }

    internal sealed class MapRenderResult
    {
        public int Width { get; }
        public int Height { get; }
        public int[] Pixels { get; }
        public int WorldMaxX { get; }
        public int WorldMaxY { get; }
        public int ViewMinX { get; }
        public int ViewMinY { get; }
        public int ViewMaxX { get; }
        public int ViewMaxY { get; }
        public int PlayerTileX { get; }
        public int PlayerTileY { get; }
        public List<MapOverlayPoint> Markers { get; }

        public MapRenderResult(
            int width, int height, int[] pixels,
            int worldMaxX, int worldMaxY,
            int viewMinX, int viewMinY, int viewMaxX, int viewMaxY,
            int playerTileX, int playerTileY,
            List<MapOverlayPoint> markers)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            WorldMaxX = worldMaxX;
            WorldMaxY = worldMaxY;
            ViewMinX = viewMinX;
            ViewMinY = viewMinY;
            ViewMaxX = viewMaxX;
            ViewMaxY = viewMaxY;
            PlayerTileX = playerTileX;
            PlayerTileY = playerTileY;
            Markers = markers;
        }
    }

    internal static class WorldMapScanner
    {
        private const int SafeMargin = 42;
        private static volatile bool _scanInFlight;

        public static void RequestScan(
            int width,
            int height,
            float zoom,
            int centerTileX,
            int centerTileY,
            Action<MapRenderResult> onCompleted)
        {
            if (_scanInFlight || width <= 8 || height <= 8)
            {
                return;
            }

            _scanInFlight = true;
            GameDispatcher.Enqueue(() =>
            {
                try
                {
                    if (!GameState.TryGetLocalPlayer(out Player? player) || player == null || Main.tile == null)
                    {
                        return;
                    }

                    int maxX = Main.maxTilesX;
                    int maxY = Main.maxTilesY;
                    if (maxX <= SafeMargin * 2 || maxY <= SafeMargin * 2)
                    {
                        return;
                    }

                    int validMinX = SafeMargin;
                    int validMaxX = maxX - SafeMargin - 1;
                    int validMinY = SafeMargin;
                    int validMaxY = maxY - SafeMargin - 1;

                    int totalW = validMaxX - validMinX;
                    int totalH = validMaxY - validMinY;

                    float clampedZoom = Math.Max(1f, Math.Min(16f, zoom));
                    int viewW = Math.Max(60, (int)(totalW / clampedZoom));
                    int viewH = Math.Max(30, (int)(totalH / clampedZoom));

                    int cx = centerTileX > 0 ? centerTileX : (int)(player.Center.X / 16f);
                    int cy = centerTileY > 0 ? centerTileY : (int)(player.Center.Y / 16f);

                    int viewMinX = Math.Max(validMinX, Math.Min(validMaxX - viewW, cx - viewW / 2));
                    int viewMinY = Math.Max(validMinY, Math.Min(validMaxY - viewH, cy - viewH / 2));
                    int viewMaxX = Math.Min(validMaxX, viewMinX + viewW);
                    int viewMaxY = Math.Min(validMaxY, viewMinY + viewH);

                    int spanX = Math.Max(1, viewMaxX - viewMinX);
                    int spanY = Math.Max(1, viewMaxY - viewMinY);

                    int[] pixels = new int[width * height];

                    for (int py = 0; py < height; py++)
                    {
                        int ty = viewMinY + (py * spanY) / height;
                        if (ty < validMinY) ty = validMinY;
                        else if (ty > validMaxY) ty = validMaxY;

                        int rowOffset = py * width;
                        for (int px = 0; px < width; px++)
                        {
                            int tx = viewMinX + (px * spanX) / width;
                            if (tx < validMinX) tx = validMinX;
                            else if (tx > validMaxX) tx = validMaxX;

                            try
                            {
                                MapTile mt = MapHelper.CreateMapTile(tx, ty, byte.MaxValue);
                                Color c = MapHelper.GetMapTileXnaColor(mt, tx, ty);
                                pixels[rowOffset + px] = (255 << 24) | (c.R << 16) | (c.G << 8) | c.B;
                            }
                            catch
                            {
                                pixels[rowOffset + px] = unchecked((int)0xFF10141C);
                            }
                        }
                    }

                    int pTileX = (int)(player.Center.X / 16f);
                    int pTileY = (int)(player.Center.Y / 16f);

                    var markers = new List<MapOverlayPoint>(32)
                    {
                        new MapOverlayPoint(MapMarkerKind.WorldSpawn, Main.spawnTileX, Main.spawnTileY, "World Spawn")
                    };

                    if (player.SpawnX >= 0 && player.SpawnY >= 0)
                    {
                        markers.Add(new MapOverlayPoint(MapMarkerKind.BedSpawn, player.SpawnX, player.SpawnY, "Bed Spawn"));
                    }

                    if (Main.dungeonX > 0 && Main.dungeonY > 0)
                    {
                        markers.Add(new MapOverlayPoint(MapMarkerKind.Dungeon, Main.dungeonX, Main.dungeonY, "Dungeon"));
                    }

                    if (player.showLastDeath)
                    {
                        int dx = (int)(player.lastDeathPostion.X / 16f);
                        int dy = (int)(player.lastDeathPostion.Y / 16f);
                        if (dx > 0 && dy > 0)
                        {
                            markers.Add(new MapOverlayPoint(MapMarkerKind.LastDeath, dx, dy, "Last Death"));
                        }
                    }

                    if (Main.npc != null)
                    {
                        for (int i = 0; i < Main.npc.Length; i++)
                        {
                            NPC? n = Main.npc[i];
                            if (n == null || !n.active)
                            {
                                continue;
                            }

                            int nx = (int)(n.Center.X / 16f);
                            int ny = (int)(n.Center.Y / 16f);
                            if (n.boss)
                            {
                                markers.Add(new MapOverlayPoint(MapMarkerKind.Boss, nx, ny, n.FullName));
                            }
                            else if (n.townNPC)
                            {
                                markers.Add(new MapOverlayPoint(MapMarkerKind.TownNpc, nx, ny, n.FullName));
                            }
                        }
                    }

                    markers.Add(new MapOverlayPoint(MapMarkerKind.Player, pTileX, pTileY, player.name));

                    var result = new MapRenderResult(
                        width, height, pixels,
                        maxX, maxY,
                        viewMinX, viewMinY, viewMaxX, viewMaxY,
                        pTileX, pTileY,
                        markers);

                    onCompleted(result);
                }
                catch (Exception ex)
                {
                    ModLogger.Error("WorldMapScanner.RequestScan", ex);
                }
                finally
                {
                    _scanInFlight = false;
                }
            });
        }
    }
}

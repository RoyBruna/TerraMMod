using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerrariaModMenu.Core.Core
{
    internal readonly struct EntityChaosTelemetry
    {
        public bool InWorld { get; }
        public int HostileCount { get; }
        public int BossCount { get; }
        public int TownCount { get; }
        public int GroundItemCount { get; }
        public int SpawnRateMult { get; }

        public EntityChaosTelemetry(
            bool inWorld,
            int hostileCount,
            int bossCount,
            int townCount,
            int groundItemCount,
            int spawnRateMult)
        {
            InWorld = inWorld;
            HostileCount = hostileCount;
            BossCount = bossCount;
            TownCount = townCount;
            GroundItemCount = groundItemCount;
            SpawnRateMult = spawnRateMult;
        }
    }

    internal static class EntityChaosEngine
    {
        public static bool IsHostileNpc(NPC? npc)
        {
            if (npc == null || !npc.active || npc.friendly || npc.townNPC || npc.lifeMax <= 5)
            {
                return false;
            }

            if (npc.type >= 0 && npc.type < NPCID.Sets.CountsAsCritter.Length && NPCID.Sets.CountsAsCritter[npc.type])
            {
                return false;
            }

            return true;
        }

        public static void TickChaos()
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || Main.gameMenu)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                bool freeze = cfg.ChaosFreezeEnemies;
                bool magnet = cfg.ChaosEnemyMagnetToCursor;
                bool vacuum = cfg.ChaosItemVacuum;

                if (!freeze && !magnet && !vacuum)
                {
                    return;
                }

                if ((freeze || magnet) && Main.npc != null)
                {
                    Vector2 cursorWorld = Main.MouseWorld;
                    NPC[] npcs = Main.npc;
                    int limit = Math.Min(Main.maxNPCs, npcs.Length);

                    for (int i = 0; i < limit; i++)
                    {
                        NPC? npc = npcs[i];
                        if (!IsHostileNpc(npc) || npc == null)
                        {
                            continue;
                        }

                        bool isSlaveSegment = npc.realLife >= 0 && npc.realLife != npc.whoAmI;

                        if (magnet && !isSlaveSegment)
                        {
                            Vector2 targetPos = cursorWorld - new Vector2(npc.width * 0.5f, npc.height * 0.5f);
                            float minX = Main.leftWorld + 640f;
                            float maxX = Main.rightWorld - 640f - npc.width;
                            float minY = Main.topWorld + 640f;
                            float maxY = Main.bottomWorld - 640f - npc.height;
                            targetPos.X = Math.Max(minX, Math.Min(maxX, targetPos.X));
                            targetPos.Y = Math.Max(minY, Math.Min(maxY, targetPos.Y));

                            npc.position = Vector2.Lerp(npc.position, targetPos, 0.28f);
                            npc.velocity = Vector2.Zero;
                        }
                        else if (freeze)
                        {
                            npc.velocity = Vector2.Zero;
                            if (!isSlaveSegment && npc.oldPosition != Vector2.Zero)
                            {
                                npc.position = npc.oldPosition;
                            }
                        }
                    }
                }

                if (vacuum && Main.item != null && GameState.TryGetLocalPlayer(out Player? player) && player != null && !player.dead && !player.ghost)
                {
                    Vector2 playerCenter = player.Center;
                    WorldItem[] items = Main.item;
                    int limit = Math.Min(Main.maxItems, items.Length);

                    for (int i = 0; i < limit; i++)
                    {
                        WorldItem? wi = items[i];
                        if (wi == null || !wi.active || wi.IsAir || wi.stack <= 0)
                        {
                            continue;
                        }

                        wi.grabDelayTime = 0;
                        Vector2 target = playerCenter - new Vector2(wi.width * 0.5f, wi.height * 0.5f);
                        float distSq = Vector2.DistanceSquared(wi.position, target);
                        if (distSq <= 784f)
                        {
                            wi.position = target;
                        }
                        else
                        {
                            wi.position = Vector2.Lerp(wi.position, target, 0.35f);
                        }
                        wi.velocity = Vector2.Zero;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("EntityChaosEngine.TickChaos", ex);
            }
        }

        public static EntityChaosTelemetry CaptureTelemetry()
        {
            try
            {
                if (!GameState.TryGetLocalPlayer(out _))
                {
                    return default;
                }

                int hostiles = 0;
                int bosses = 0;
                int town = 0;

                NPC[]? npcs = Main.npc;
                if (npcs != null)
                {
                    int limit = Math.Min(Main.maxNPCs, npcs.Length);
                    for (int i = 0; i < limit; i++)
                    {
                        NPC? n = npcs[i];
                        if (n == null || !n.active)
                        {
                            continue;
                        }

                        if (n.boss)
                        {
                            bosses++;
                            hostiles++;
                        }
                        else if (n.townNPC)
                        {
                            town++;
                        }
                        else if (IsHostileNpc(n))
                        {
                            hostiles++;
                        }
                    }
                }

                int itemsCount = 0;
                WorldItem[]? items = Main.item;
                if (items != null)
                {
                    int limit = Math.Min(Main.maxItems, items.Length);
                    for (int i = 0; i < limit; i++)
                    {
                        WorldItem? wi = items[i];
                        if (wi != null && wi.active && !wi.IsAir && wi.stack > 0)
                        {
                            itemsCount++;
                        }
                    }
                }

                return new EntityChaosTelemetry(
                    true,
                    hostiles,
                    bosses,
                    town,
                    itemsCount,
                    ModConfig.Instance.SpawnRateMultiplier);
            }
            catch (Exception ex)
            {
                ModLogger.Error("EntityChaosEngine.CaptureTelemetry", ex);
                return default;
            }
        }
    }
}

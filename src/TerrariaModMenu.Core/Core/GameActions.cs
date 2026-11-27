using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Testing;

namespace TerrariaModMenu.Core.Core
{
    internal enum NpcCatalogCategory
    {
        Hostile = 0,
        Boss = 1,
        Town = 2,
        Critter = 3
    }

    internal readonly struct NpcCatalogEntry
    {
        public int Id { get; }
        public string Name { get; }
        public NpcCatalogCategory Category { get; }

        public NpcCatalogEntry(int id, string name, NpcCatalogCategory category)
        {
            Id = id;
            Name = name;
            Category = category;
        }

        public override string ToString()
        {
            string tag = Category switch
            {
                NpcCatalogCategory.Boss => "BOSS",
                NpcCatalogCategory.Town => "TOWN",
                NpcCatalogCategory.Critter => "CRIT",
                _ => "MOB "
            };
            return $"[{tag}] [{Id}] {Name}";
        }
    }

    internal readonly struct CatalogEntry
    {
        public int Id { get; }
        public string Name { get; }

        public CatalogEntry(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => $"[{Id}] {Name}";
    }

    internal readonly struct SummonerTelemetry
    {
        public bool InWorld { get; }
        public int ActiveMinions { get; }
        public float UsedMinionSlots { get; }
        public int MaxMinions { get; }
        public int ActiveSentries { get; }
        public int MaxSentries { get; }
        public float MinionDamageMult { get; }
        public float WhipRangeMult { get; }
        public float WhipUseTimeMult { get; }

        public SummonerTelemetry(
            bool inWorld,
            int activeMinions,
            float usedMinionSlots,
            int maxMinions,
            int activeSentries,
            int maxSentries,
            float minionDamageMult,
            float whipRangeMult,
            float whipUseTimeMult)
        {
            InWorld = inWorld;
            ActiveMinions = activeMinions;
            UsedMinionSlots = usedMinionSlots;
            MaxMinions = maxMinions;
            ActiveSentries = activeSentries;
            MaxSentries = maxSentries;
            MinionDamageMult = minionDamageMult;
            WhipRangeMult = whipRangeMult;
            WhipUseTimeMult = whipUseTimeMult;
        }
    }

    internal static class GameActions
    {
        private static readonly HashSet<int> ModifiedItemSlots = new HashSet<int>();
        private static readonly int[] CombatPackBuffs =
        {
            BuffID.Ironskin,
            BuffID.Endurance,
            BuffID.Regeneration,
            BuffID.Lifeforce,
            BuffID.Wrath,
            BuffID.Rage,
            BuffID.Swiftness,
            BuffID.WellFed3,
            BuffID.MagicPower,
            BuffID.ManaRegeneration,
            BuffID.Archery,
            BuffID.Inferno,
            BuffID.Thorns,
            BuffID.Heartreach,
            BuffID.Titan
        };

        private static readonly int[] ExplorerPackBuffs =
        {
            BuffID.Spelunker,
            BuffID.Hunter,
            BuffID.Dangersense,
            BuffID.NightOwl,
            BuffID.Shine,
            BuffID.Mining,
            BuffID.Builder,
            BuffID.ObsidianSkin,
            BuffID.WaterWalking,
            BuffID.Featherfall,
            BuffID.BiomeSight,
            BuffID.Swiftness
        };

        public static List<CatalogEntry> BuildItemCatalog()
        {
            var list = new List<CatalogEntry>(4096);
            try
            {
                int count = ItemID.Count;
                for (int id = 1; id < count; id++)
                {
                    string name = Lang.GetItemNameValue(id);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        list.Add(new CatalogEntry(id, name));
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameActions.BuildItemCatalog", ex);
            }
            return list;
        }

        public static List<CatalogEntry> BuildBuffCatalog()
        {
            var list = new List<CatalogEntry>(512);
            try
            {
                int count = BuffID.Count;
                for (int id = 1; id < count; id++)
                {
                    string name = Lang.GetBuffName(id);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        list.Add(new CatalogEntry(id, name));
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameActions.BuildBuffCatalog", ex);
            }
            return list;
        }

        public static List<CatalogEntry> BuildPrefixCatalog()
        {
            return new List<CatalogEntry>
            {
                new CatalogEntry(0, "None"),
                new CatalogEntry(-1, "Random"),
                new CatalogEntry(PrefixID.Legendary, "Legendary"),
                new CatalogEntry(PrefixID.Unreal, "Unreal"),
                new CatalogEntry(PrefixID.Mythical, "Mythical"),
                new CatalogEntry(PrefixID.Ruthless, "Ruthless"),
                new CatalogEntry(PrefixID.Godly, "Godly"),
                new CatalogEntry(PrefixID.Demonic, "Demonic"),
                new CatalogEntry(PrefixID.Light, "Light"),
                new CatalogEntry(PrefixID.Menacing, "Menacing"),
                new CatalogEntry(PrefixID.Warding, "Warding"),
                new CatalogEntry(PrefixID.Lucky, "Lucky"),
                new CatalogEntry(PrefixID.Quick, "Quick"),
                new CatalogEntry(PrefixID.Violent, "Violent")
            };
        }

        public static void EnqueueRefreshRecipes()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (GameState.TryGetLocalPlayer(out _))
                {
                    Recipe.UpdateRecipeList();
                }
            });
        }

        public static void EnqueueSpawnItem(int itemId, int stack, int prefixId)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                var item = new Item();
                item.SetDefaults(itemId);
                if (item.IsAir)
                {
                    return;
                }

                if (prefixId != 0)
                {
                    item.Prefix(prefixId);
                }

                int clampedStack = Math.Max(1, Math.Min(stack, Math.Max(1, item.maxStack)));
                item.stack = clampedStack;
                player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), item);
                Main.NewText($"[ModMenu] Spawned {clampedStack}x {item.Name}", 110, 231, 183);
            });
        }

        public static void EnqueueModifyHeldItem(
            int damage, int crit, int useTime, int useAnimation,
            float shootSpeed, float knockBack, float scale, bool autoReuse)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                Item held = player.HeldItem;
                if (held == null || held.IsAir)
                {
                    return;
                }

                held.damage = Math.Max(0, damage);
                held.crit = crit;
                held.useTime = Math.Max(1, useTime);
                held.useAnimation = Math.Max(1, useAnimation);
                held.shootSpeed = shootSpeed;
                held.knockBack = knockBack;
                held.scale = Math.Max(0.1f, scale);
                held.autoReuse = autoReuse;
                ModifiedItemSlots.Add(player.selectedItem);
                Main.NewText($"[ModMenu] Updated {held.Name}", 96, 165, 250);
            });
        }

        public static void EnqueueResetHeldItem()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                Item held = player.HeldItem;
                if (held == null || held.IsAir)
                {
                    return;
                }

                int type = held.type;
                int stack = held.stack;
                byte prefix = held.prefix;
                held.SetDefaults(type);
                held.stack = stack;
                if (prefix > 0)
                {
                    held.Prefix(prefix);
                }
                ModifiedItemSlots.Remove(player.selectedItem);
                Main.NewText($"[ModMenu] Restored {held.Name}", 251, 191, 36);
            });
        }

        public static void EnqueueApplyBuff(int buffId, int durationTicks)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                player.AddBuff(buffId, Math.Max(60, durationTicks), false);
            });
        }

        public static void EnqueueApplyCombatPack(int durationTicks)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                foreach (int id in CombatPackBuffs)
                {
                    player.AddBuff(id, durationTicks, false);
                }
                Main.NewText("[ModMenu] Combat buff pack applied", 248, 113, 113);
            });
        }

        public static void EnqueueApplyExplorerPack(int durationTicks)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                foreach (int id in ExplorerPackBuffs)
                {
                    player.AddBuff(id, durationTicks, false);
                }
                Main.NewText("[ModMenu] Explorer buff pack applied", 52, 211, 153);
            });
        }

        public static void EnqueuePurgeDebuffs()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                PurgeDebuffsImmediate(player);
                player.potionDelay = 0;
                player.manaPotionDelay = 0;
                Main.NewText("[ModMenu] Debuffs cleared", 96, 165, 250);
            });
        }

        public static void PurgeDebuffsImmediate(Player player)
        {
            if (player.buffType == null || player.buffTime == null || Main.debuff == null)
            {
                return;
            }

            for (int i = player.buffType.Length - 1; i >= 0; i--)
            {
                int type = player.buffType[i];
                if (type > 0 && type < Main.debuff.Length && Main.debuff[type])
                {
                    player.DelBuff(i);
                }
            }
        }

        public static void EnqueueRevealFullMap()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _) || Main.Map == null)
                {
                    return;
                }

                DebugOptions.unlockMap = 2;
                Main.sectionManager?.ClearMapDraw();
                Main.refreshMap = true;
                Main.updateMap = new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY);
                Main.NewText("[ModMenu] Revealing full world map...", 250, 204, 21);
            });
        }

        public static void EnqueueClearMap()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _) || Main.Map == null)
                {
                    return;
                }

                DebugOptions.unlockMap = 0;
                Main.Map.Clear();
                Main.clearMap = true;
                Main.sectionManager?.ClearMapDraw();
                Main.refreshMap = true;
                Main.updateMap = new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY);
                Main.NewText("[ModMenu] World map cleared", 156, 163, 175);
            });
        }

        public static void EnqueueTeleportToCursor()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                Vector2 target = Main.MouseWorld - new Vector2(player.width * 0.5f, player.height * 0.5f);
                player.Teleport(target, 1, 0);
                player.velocity = Vector2.Zero;
            });
        }

        public static void EnqueueTeleportToSpawn()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                Vector2 spawn = new Vector2(Main.spawnTileX * 16f, (Main.spawnTileY - 3) * 16f);
                player.Teleport(spawn, 1, 0);
                player.velocity = Vector2.Zero;
                Main.NewText("[ModMenu] Teleported to spawn", 96, 165, 250);
            });
        }

        public static void EnqueueTeleportToTile(int tileX, int tileY, string? label = null)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                int clampedX = Math.Max(45, Math.Min(Main.maxTilesX - 45, tileX));
                int clampedY = Math.Max(45, Math.Min(Main.maxTilesY - 45, tileY));
                Vector2 target = new Vector2(clampedX * 16f, (clampedY - 2) * 16f);
                player.Teleport(target, 1, 0);
                player.velocity = Vector2.Zero;
                player.fallStart = clampedY;

                string tag = string.IsNullOrWhiteSpace(label) ? $"({clampedX}, {clampedY})" : $"{label} ({clampedX}, {clampedY})";
                Main.NewText($"[ModMenu] Teleported to {tag}", 110, 231, 183);
            });
        }

        public static void EnqueueTeleportLandmark(string landmark)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                int tx = Main.spawnTileX;
                int ty = Main.spawnTileY;

                switch (landmark)
                {
                    case "Spawn":
                        tx = Main.spawnTileX;
                        ty = Main.spawnTileY;
                        break;
                    case "Bed":
                        if (player.SpawnX >= 0 && player.SpawnY >= 0)
                        {
                            tx = player.SpawnX;
                            ty = player.SpawnY;
                        }
                        break;
                    case "Dungeon":
                        if (Main.dungeonX > 0 && Main.dungeonY > 0)
                        {
                            tx = Main.dungeonX;
                            ty = Main.dungeonY;
                        }
                        break;
                    case "Death":
                        if (player.showLastDeath)
                        {
                            tx = (int)(player.lastDeathPostion.X / 16f);
                            ty = (int)(player.lastDeathPostion.Y / 16f);
                        }
                        break;
                    case "LeftOcean":
                        tx = 180;
                        ty = Math.Max(120, (int)Main.worldSurface - 20);
                        break;
                    case "RightOcean":
                        tx = Main.maxTilesX - 180;
                        ty = Math.Max(120, (int)Main.worldSurface - 20);
                        break;
                    case "Hell":
                        tx = Main.maxTilesX / 2;
                        ty = Main.maxTilesY - 140;
                        break;
                }

                int clampedX = Math.Max(45, Math.Min(Main.maxTilesX - 45, tx));
                int clampedY = Math.Max(45, Math.Min(Main.maxTilesY - 45, ty));
                player.Teleport(new Vector2(clampedX * 16f, (clampedY - 2) * 16f), 1, 0);
                player.velocity = Vector2.Zero;
                player.fallStart = clampedY;
                Main.NewText($"[ModMenu] Teleported to {landmark} ({clampedX}, {clampedY})", 96, 165, 250);
            });
        }

        public static void EnqueueSetTime(bool dayTime, double time)
        {
            GameDispatcher.Enqueue(() =>
            {
                Main.dayTime = dayTime;
                Main.time = time;
                ModConfig.Instance.FrozenDayTime = dayTime;
                ModConfig.Instance.FrozenTimeValue = time;
            });
        }

        public static void EnqueueFullHeal()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                player.statLife = player.statLifeMax2;
                player.statMana = player.statManaMax2;
                player.HealEffect(player.statLifeMax2, true);
            });
        }

        public static void EnqueueRerollAnglerQuest()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _))
                {
                    return;
                }

                Main.AnglerQuestSwap();
                int id = (Main.anglerQuestItemNetIDs != null && Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
                    ? Main.anglerQuestItemNetIDs[Main.anglerQuest]
                    : 0;
                string name = id > 0 ? Lang.GetItemNameValue(id) : "Unknown";
                Main.NewText($"[ModMenu] New Angler Quest: {name}", 56, 189, 248);
            });
        }

        public static void EnqueueResetAnglerDaily()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _))
                {
                    return;
                }

                Main.anglerQuestFinished = false;
                Main.anglerWhoFinishedToday?.Clear();
                Main.NewText("[ModMenu] Angler daily quest limit reset", 34, 197, 94);
            });
        }

        public static void EnqueueSpawnCurrentQuestFish()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null || Main.anglerQuestItemNetIDs == null)
                {
                    return;
                }

                if (Main.anglerQuest < 0 || Main.anglerQuest >= Main.anglerQuestItemNetIDs.Length)
                {
                    return;
                }

                int fishId = Main.anglerQuestItemNetIDs[Main.anglerQuest];
                var item = new Item();
                item.SetDefaults(fishId);
                if (!item.IsAir)
                {
                    item.stack = 1;
                    player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), item);
                    Main.NewText($"[ModMenu] Spawned Quest Fish: {item.Name}", 250, 204, 21);
                }
            });
        }

        public static void EnqueueApplyFishingBuffs()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                int ticks = 30 * 60 * 60;
                player.AddBuff(BuffID.Fishing, ticks, false);
                player.AddBuff(BuffID.Sonar, ticks, false);
                player.AddBuff(BuffID.Crate, ticks, false);
                player.AddBuff(BuffID.Lucky, ticks, false);
                Main.NewText("[ModMenu] Fishing buffs applied (30m)", 139, 92, 246);
            });
        }

        public static void EnqueueGiveMasterFishingKit()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                GiveKitItem(player, ItemID.GoldenFishingRod, 1);
                GiveKitItem(player, ItemID.MasterBait, 99);
                GiveKitItem(player, ItemID.LavaproofTackleBag, 1);
                Main.NewText("[ModMenu] Master Fishing Kit added", 34, 197, 94);
            });
        }

        public static void EnqueueGiveAnglerArmorSet()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                GiveKitItem(player, ItemID.AnglerHat, 1);
                GiveKitItem(player, ItemID.AnglerVest, 1);
                GiveKitItem(player, ItemID.AnglerPants, 1);
                Main.NewText("[ModMenu] Angler Armor Set added", 96, 165, 250);
            });
        }

        public static void EnqueueReelAllBobbers()
        {
            GameDispatcher.Enqueue(TerrariaModMenu.Core.Patches.FishingPatches.ReelAllActiveBobbersNow);
        }

        public static void EnqueueResetFishingState()
        {
            GameDispatcher.Enqueue(() =>
            {
                TerrariaModMenu.Core.Patches.FishingPatches.ResetState();
                if (GameState.TryGetLocalPlayer(out Player? player) && player != null)
                {
                    player.RemoveAllFishingBobbers();
                    Main.NewText("[ModMenu] Fishing restored to vanilla defaults", 56, 189, 248);
                }
            });
        }

        public static void EnqueueRestoreVanillaState()
        {
            GameDispatcher.Enqueue(() => RestoreVanillaStateImmediate(true));
        }

        public static void RestoreVanillaStateImmediate(bool announce)
        {
            try
            {
                TerrariaModMenu.Core.Patches.FishingPatches.ResetState();
                Main.dayRate = 1;
                Player.tileRangeX = Player.DefaultTileRangeX;
                Player.tileRangeY = Player.DefaultTileRangeY;
                DebugOptions.unlockMap = 0;

                if (GameState.TryGetLocalPlayer(out Player? player) && player != null)
                {
                    player.creativeGodMode = false;
                    player.immune = false;
                    player.immuneNoBlink = false;
                    player.immuneTime = 0;
                    player.RemoveAllFishingBobbers();

                    if (ModifiedItemSlots.Count > 0 && player.inventory != null)
                    {
                        foreach (int slot in ModifiedItemSlots)
                        {
                            if (slot >= 0 && slot < player.inventory.Length)
                            {
                                Item it = player.inventory[slot];
                                if (it != null && !it.IsAir && it.type > 0)
                                {
                                    int t = it.type;
                                    int s = it.stack;
                                    byte p = it.prefix;
                                    it.SetDefaults(t);
                                    it.stack = s;
                                    if (p > 0)
                                    {
                                        it.Prefix(p);
                                    }
                                }
                            }
                        }
                        ModifiedItemSlots.Clear();
                    }

                    player.UpdateMaxTurrets();
                    Recipe.UpdateRecipeList();

                    if (announce)
                    {
                        Main.NewText("[ModMenu] All modifications cleared - vanilla state restored", 34, 197, 94);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameActions.RestoreVanillaStateImmediate", ex);
            }
        }

        public static void EnqueueDespawnAllMinions()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null || Main.projectile == null)
                {
                    return;
                }

                int killed = 0;
                int limit = Math.Min(Main.maxProjectiles, Main.projectile.Length);
                for (int i = 0; i < limit; i++)
                {
                    Projectile? p = Main.projectile[i];
                    if (p != null && p.active && p.owner == Main.myPlayer && p.minion)
                    {
                        p.Kill();
                        killed++;
                    }
                }
                Main.NewText($"[ModMenu] Despawned {killed} minion(s)", 139, 92, 246);
            });
        }

        public static void EnqueueDespawnAllSentries()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null || Main.projectile == null)
                {
                    return;
                }

                int killed = 0;
                int limit = Math.Min(Main.maxProjectiles, Main.projectile.Length);
                for (int i = 0; i < limit; i++)
                {
                    Projectile? p = Main.projectile[i];
                    if (p != null && p.active && p.owner == Main.myPlayer && p.sentry)
                    {
                        p.Kill();
                        killed++;
                    }
                }
                Main.NewText($"[ModMenu] Despawned {killed} sentry/turret(s)", 96, 165, 250);
            });
        }

        public static void EnqueueApplySummonerBuffs()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                int ticks = 30 * 60 * 60;
                player.AddBuff(BuffID.Summoning, ticks, false);
                player.AddBuff(BuffID.Bewitched, ticks, false);
                player.AddBuff(BuffID.WarTable, ticks, false);
                player.AddBuff(BuffID.WellFed3, ticks, false);
                Main.NewText("[ModMenu] Summoner buff pack applied (30m)", 139, 92, 246);
            });
        }

        public static SummonerTelemetry CaptureSummonerTelemetry()
        {
            try
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return default;
                }

                int minions = 0;
                float usedSlots = 0f;
                int sentries = 0;
                Projectile[]? arr = Main.projectile;
                if (arr != null)
                {
                    int limit = Math.Min(Main.maxProjectiles, arr.Length);
                    for (int i = 0; i < limit; i++)
                    {
                        Projectile? p = arr[i];
                        if (p == null || !p.active || p.owner != Main.myPlayer)
                        {
                            continue;
                        }

                        if (p.minion)
                        {
                            minions++;
                            usedSlots += p.minionSlots;
                        }
                        else if (p.sentry)
                        {
                            sentries++;
                        }
                    }
                }

                return new SummonerTelemetry(
                    true,
                    minions,
                    usedSlots,
                    player.maxMinions,
                    sentries,
                    player.maxTurrets,
                    player.minionDamage,
                    player.whipRangeMultiplier,
                    player.whipUseTimeMultiplier);
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameActions.CaptureSummonerTelemetry", ex);
                return default;
            }
        }

        public static List<NpcCatalogEntry> BuildNpcCatalog()
        {
            var list = new List<NpcCatalogEntry>(700);
            try
            {
                int count = NPCID.Count;
                var probe = new NPC();
                for (int id = 1; id < count; id++)
                {
                    string name = Lang.GetNPCNameValue(id);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    NpcCatalogCategory cat = NpcCatalogCategory.Hostile;
                    try
                    {
                        probe.SetDefaults(id);
                        bool isCritter = id < NPCID.Sets.CountsAsCritter.Length && NPCID.Sets.CountsAsCritter[id];
                        bool isBossSet = id < NPCID.Sets.ShouldBeCountedAsBossForBestiary.Length && NPCID.Sets.ShouldBeCountedAsBossForBestiary[id];
                        if (probe.boss || isBossSet || id == NPCID.EaterofWorldsHead || id == NPCID.MoonLordCore)
                        {
                            cat = NpcCatalogCategory.Boss;
                        }
                        else if (probe.townNPC)
                        {
                            cat = NpcCatalogCategory.Town;
                        }
                        else if (isCritter || (probe.friendly && !probe.townNPC))
                        {
                            cat = NpcCatalogCategory.Critter;
                        }
                    }
                    catch
                    {
                    }

                    list.Add(new NpcCatalogEntry(id, name, cat));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameActions.BuildNpcCatalog", ex);
            }
            return list;
        }

        public static void EnqueueSpawnNpc(int npcType, int count, int locationMode)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                int clampedCount = Math.Max(1, Math.Min(100, count));

                if (npcType == -999)
                {
                    for (int k = 0; k < Math.Min(clampedCount, 10); k++)
                    {
                        SpawnSingleNpc(player, NPCID.Retinazer, locationMode, k, 2);
                        SpawnSingleNpc(player, NPCID.Spazmatism, locationMode, k + 1, 2);
                    }
                    Main.NewText("[ModMenu] Spawned The Twins", 248, 113, 113);
                    return;
                }

                if (npcType == NPCID.WallofFlesh)
                {
                    NPC.SpawnWOF(locationMode == 1 ? Main.MouseWorld : player.position);
                    Main.NewText("[ModMenu] Spawned Wall of Flesh", 248, 113, 113);
                    return;
                }

                int spawned = 0;
                for (int k = 0; k < clampedCount; k++)
                {
                    if (SpawnSingleNpc(player, npcType, locationMode, k, clampedCount))
                    {
                        spawned++;
                    }
                }

                string npcName = Lang.GetNPCNameValue(npcType);
                if (string.IsNullOrWhiteSpace(npcName))
                {
                    npcName = $"NPC #{npcType}";
                }

                if (spawned > 0)
                {
                    Main.NewText($"[ModMenu] Spawned {spawned}x {npcName}", 250, 204, 21);
                }
            });
        }

        private static bool SpawnSingleNpc(Player player, int npcType, int locationMode, int index, int total)
        {
            if (locationMode == 2 && npcType != NPCID.Golem && npcType != NPCID.DukeFishron)
            {
                NPC.SpawnOnPlayer(Main.myPlayer, npcType);
                return true;
            }

            Vector2 origin;
            if (locationMode == 1)
            {
                origin = Main.MouseWorld;
            }
            else
            {
                float dir = player.direction == 0 ? 1f : player.direction;
                origin = player.Center + new Vector2(dir * 160f, -96f);
            }

            if (total > 1)
            {
                double angle = (index / (double)total) * Math.PI * 2.0;
                float radius = Math.Min(180f, 24f + (index % 8) * 16f);
                origin.X += (float)(Math.Cos(angle) * radius);
                origin.Y += (float)(Math.Sin(angle) * radius * 0.45f);
            }

            int x = (int)Math.Max(Main.leftWorld + 700f, Math.Min(Main.rightWorld - 700f, origin.X));
            int y = (int)Math.Max(Main.topWorld + 700f, Math.Min(Main.bottomWorld - 700f, origin.Y));

            IEntitySource source = NPC.GetBossSpawnSource(Main.myPlayer);
            int idx = NPC.NewNPC(source, x, y, npcType, 0, 0f, 0f, 0f, 0f, Main.myPlayer);
            return idx >= 0 && idx < Main.maxNPCs;
        }

        public static void EnqueueButcherHostiles(bool includeBosses)
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _) || Main.npc == null)
                {
                    return;
                }

                int killed = 0;
                NPC[] npcs = Main.npc;
                int limit = Math.Min(Main.maxNPCs, npcs.Length);

                for (int i = 0; i < limit; i++)
                {
                    NPC? npc = npcs[i];
                    if (!EntityChaosEngine.IsHostileNpc(npc) || npc == null)
                    {
                        continue;
                    }

                    if (!includeBosses && (npc.boss || (npc.type < NPCID.Sets.ShouldBeCountedAsBossForBestiary.Length && NPCID.Sets.ShouldBeCountedAsBossForBestiary[npc.type])))
                    {
                        continue;
                    }

                    npc.dontTakeDamage = false;
                    npc.immortal = false;
                    int dmg = Math.Max(9999999, npc.lifeMax * 10);
                    npc.StrikeNPC(dmg, 0f, 0, true, false, Main.myPlayer);
                    if (npc.active && npc.life > 0)
                    {
                        npc.life = 0;
                        npc.checkDead();
                    }
                    killed++;
                }

                string scope = includeBosses ? "hostiles + bosses" : "hostile NPCs";
                Main.NewText($"[ModMenu] Butchered {killed} {scope} (with loot)", 248, 113, 113);
            });
        }

        public static void EnqueueClearGroundItems()
        {
            GameDispatcher.Enqueue(() =>
            {
                if (!GameState.TryGetLocalPlayer(out _) || Main.item == null)
                {
                    return;
                }

                int cleared = 0;
                WorldItem[] items = Main.item;
                int limit = Math.Min(Main.maxItems, items.Length);
                for (int i = 0; i < limit; i++)
                {
                    WorldItem? wi = items[i];
                    if (wi != null && wi.active && !wi.IsAir)
                    {
                        wi.TurnToAir();
                        cleared++;
                    }
                }
                Main.NewText($"[ModMenu] Cleared {cleared} ground item(s)", 156, 163, 175);
            });
        }

        private static void GiveKitItem(Player player, int itemId, int stack)
        {
            var item = new Item();
            item.SetDefaults(itemId);
            if (!item.IsAir)
            {
                item.stack = Math.Max(1, Math.Min(stack, Math.Max(1, item.maxStack)));
                player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), item);
            }
        }
    }
}

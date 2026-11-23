using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Testing;

namespace TerrariaModMenu.Core.Core
{
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

    internal static class GameActions
    {
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
    }
}

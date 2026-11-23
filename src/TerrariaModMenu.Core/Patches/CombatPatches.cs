using System;
using HarmonyLib;
using Terraria;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class CombatPatches
    {
        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyItemTime(Item)",
                AccessTools.Method(typeof(Player), nameof(Player.ApplyItemTime), new[] { typeof(Item) }),
                postfix: AccessTools.Method(typeof(CombatPatches), nameof(Postfix_ApplyItemTime)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyItemTime(Item, float)",
                AccessTools.Method(typeof(Player), nameof(Player.ApplyItemTime), new[] { typeof(Item), typeof(float) }),
                postfix: AccessTools.Method(typeof(CombatPatches), nameof(Postfix_ApplyItemTime)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyItemAnimation(Item)",
                AccessTools.Method(typeof(Player), "ApplyItemAnimation", new[] { typeof(Item) }),
                postfix: AccessTools.Method(typeof(CombatPatches), nameof(Postfix_ApplyItemAnimation)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyReuseDelay",
                AccessTools.Method(typeof(Player), "ApplyReuseDelay", Type.EmptyTypes),
                prefix: AccessTools.Method(typeof(CombatPatches), nameof(Prefix_ApplyReuseDelay)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.TryAllowingItemReuse",
                AccessTools.Method(typeof(Player), "TryAllowingItemReuse", new[] { typeof(Item) }),
                postfix: AccessTools.Method(typeof(CombatPatches), nameof(Postfix_TryAllowingItemReuse)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.PickAmmo",
                AccessTools.Method(typeof(Player), nameof(Player.PickAmmo)),
                prefix: AccessTools.Method(typeof(CombatPatches), nameof(Prefix_PickAmmo)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.CanConsumeConsumableItem",
                AccessTools.Method(typeof(Player), nameof(Player.CanConsumeConsumableItem), new[] { typeof(Item) }),
                prefix: AccessTools.Method(typeof(CombatPatches), nameof(Prefix_CanConsumeConsumableItem)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ItemCheck_PayMana_ShouldSkipManaUse",
                AccessTools.Method(typeof(Player), "ItemCheck_PayMana_ShouldSkipManaUse", new[] { typeof(Item), typeof(bool) }),
                prefix: AccessTools.Method(typeof(CombatPatches), nameof(Prefix_ShouldSkipManaUse)));
        }

        private static void Postfix_ApplyItemTime(Player __instance, Item sItem)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || sItem == null)
                {
                    return;
                }

                ItemCategory cat = ItemClassifier.Classify(sItem);
                if (ModConfig.Instance.TryGetRapidFireTicks(cat, out int maxTicks))
                {
                    int clamped = Math.Max(1, maxTicks);
                    if (__instance.itemTime > clamped)
                    {
                        __instance.itemTime = clamped;
                        __instance.itemTimeMax = clamped;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.ApplyItemTime", ex);
            }
        }

        private static void Postfix_ApplyItemAnimation(Player __instance, Item sItem)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || sItem == null)
                {
                    return;
                }

                ItemCategory cat = ItemClassifier.Classify(sItem);
                if (ModConfig.Instance.TryGetRapidFireTicks(cat, out int maxTicks))
                {
                    int clamped = Math.Max(1, maxTicks);
                    if (__instance.itemAnimation > clamped)
                    {
                        __instance.itemAnimation = clamped;
                        __instance.itemAnimationMax = clamped;
                    }
                    __instance.reuseDelay = 0;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.ApplyItemAnimation", ex);
            }
        }

        private static bool Prefix_ApplyReuseDelay(Player __instance)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer)
                {
                    return true;
                }

                if (ModConfig.Instance.TryGetRapidFireTicks(ItemClassifier.Classify(__instance.HeldItem), out _))
                {
                    __instance.reuseDelay = 0;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.ApplyReuseDelay", ex);
            }
            return true;
        }

        private static void Postfix_TryAllowingItemReuse(Player __instance, Item sItem)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || sItem == null)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                ItemCategory cat = ItemClassifier.Classify(sItem);
                if ((cfg.ForceAutoReuseAll || cfg.TryGetRapidFireTicks(cat, out _)) &&
                    (!sItem.channel || !__instance.channel))
                {
                    __instance.releaseUseItem = true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.TryAllowingItemReuse", ex);
            }
        }

        private static void Prefix_PickAmmo(Player __instance, ref bool dontConsume)
        {
            try
            {
                if (ModRuntime.IsFullModeActive && __instance != null && __instance.whoAmI == Main.myPlayer && ModConfig.Instance.InfiniteAmmo)
                {
                    dontConsume = true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.PickAmmo", ex);
            }
        }

        private static bool Prefix_CanConsumeConsumableItem(Player __instance, Item item, ref bool __result)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || item == null)
                {
                    return true;
                }

                ModConfig cfg = ModConfig.Instance;
                ItemCategory cat = ItemClassifier.Classify(item);

                bool preserve =
                    (cfg.InfiniteThrowables && cat == ItemCategory.Throwable) ||
                    (cfg.InfinitePotions && (cat == ItemCategory.Potion || item.buffType > 0)) ||
                    (cfg.InfiniteBlocks && cat == ItemCategory.BlockOrWall);

                if (preserve)
                {
                    __result = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.CanConsumeConsumableItem", ex);
            }
            return true;
        }

        private static bool Prefix_ShouldSkipManaUse(Player __instance, ref bool __result)
        {
            try
            {
                if (ModRuntime.IsFullModeActive && __instance != null && __instance.whoAmI == Main.myPlayer && ModConfig.Instance.InfiniteMana)
                {
                    __result = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CombatPatches.ShouldSkipManaUse", ex);
            }
            return true;
        }
    }
}

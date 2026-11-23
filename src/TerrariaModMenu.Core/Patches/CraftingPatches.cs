using System;
using System.Collections.Generic;
using HarmonyLib;
using Terraria;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class CraftingPatches
    {
        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "Recipe.PlayerMeetsEnvironmentConditions",
                AccessTools.Method(typeof(Recipe), nameof(Recipe.PlayerMeetsEnvironmentConditions),
                    new[] { typeof(Player), typeof(List<string>) }),
                prefix: AccessTools.Method(typeof(CraftingPatches), nameof(Prefix_PlayerMeetsEnvironmentConditions)));

            HarmonyManager.SafePatch(
                harmony,
                "Recipe.HowManyTimesCanRecipeBeCrafted",
                AccessTools.Method(typeof(Recipe), nameof(Recipe.HowManyTimesCanRecipeBeCrafted),
                    new[] { typeof(Recipe.RequiredItemEntry[]) }),
                prefix: AccessTools.Method(typeof(CraftingPatches), nameof(Prefix_HowManyTimesCanRecipeBeCrafted)));

            HarmonyManager.SafePatch(
                harmony,
                "Recipe.GetIngredientsForOneCraft",
                AccessTools.Method(typeof(Recipe), nameof(Recipe.GetIngredientsForOneCraft),
                    new[] { typeof(Player), typeof(List<Recipe.RequiredItemEntry>) }),
                prefix: AccessTools.Method(typeof(CraftingPatches), nameof(Prefix_GetIngredientsForOneCraft)));
        }

        private static bool Prefix_PlayerMeetsEnvironmentConditions(Player player, ref bool __result)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive)
                {
                    return true;
                }

                ModConfig cfg = ModConfig.Instance;
                if ((cfg.CraftIgnoreStations || cfg.CraftUnlockAllRecipes) &&
                    player != null && player.whoAmI == Main.myPlayer)
                {
                    __result = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CraftingPatches.PlayerMeetsEnvironmentConditions", ex);
            }
            return true;
        }

        private static bool Prefix_HowManyTimesCanRecipeBeCrafted(ref int __result)
        {
            try
            {
                if (ModRuntime.IsFullModeActive && ModConfig.Instance.CraftUnlockAllRecipes)
                {
                    __result = 9999;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CraftingPatches.HowManyTimesCanRecipeBeCrafted", ex);
            }
            return true;
        }

        private static bool Prefix_GetIngredientsForOneCraft(Player player)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive)
                {
                    return true;
                }

                ModConfig cfg = ModConfig.Instance;
                if ((cfg.CraftFreeNoConsume || cfg.CraftUnlockAllRecipes) &&
                    player != null && player.whoAmI == Main.myPlayer)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CraftingPatches.GetIngredientsForOneCraft", ex);
            }
            return true;
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Light;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class WorldAndVisualPatches
    {
        private static bool _inExtraSpawnPass;
        private static readonly FieldInfo? LightMapColorsField = AccessTools.Field(typeof(LightMap), "_colors");

        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "LightMap.Blur",
                AccessTools.Method(typeof(LightMap), nameof(LightMap.Blur), Type.EmptyTypes),
                prefix: AccessTools.Method(typeof(WorldAndVisualPatches), nameof(Prefix_LightMapBlur)));

            HarmonyManager.SafePatch(
                harmony,
                "NPC.SpawnNPC",
                AccessTools.Method(typeof(NPC), nameof(NPC.SpawnNPC), Type.EmptyTypes),
                prefix: AccessTools.Method(typeof(WorldAndVisualPatches), nameof(Prefix_SpawnNPC)),
                postfix: AccessTools.Method(typeof(WorldAndVisualPatches), nameof(Postfix_SpawnNPC)));
        }

        private static bool Prefix_LightMapBlur(LightMap __instance)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || !ModConfig.Instance.Fullbright ||
                    __instance == null || LightMapColorsField == null)
                {
                    return true;
                }

                if (LightMapColorsField.GetValue(__instance) is Vector3[] colors)
                {
                    Vector3 white = Vector3.One;
                    for (int i = 0; i < colors.Length; i++)
                    {
                        colors[i] = white;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("WorldAndVisualPatches.LightMapBlur", ex);
            }
            return true;
        }

        private static bool Prefix_SpawnNPC()
        {
            if (!ModRuntime.IsFullModeActive)
            {
                return true;
            }
            return ModConfig.Instance.SpawnRateMultiplier != 0;
        }

        private static void Postfix_SpawnNPC()
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || _inExtraSpawnPass || Main.gameMenu)
                {
                    return;
                }

                int mult = ModConfig.Instance.SpawnRateMultiplier;
                if (mult <= 1)
                {
                    return;
                }

                _inExtraSpawnPass = true;
                try
                {
                    int extraPasses = Math.Min(mult - 1, 19);
                    for (int i = 0; i < extraPasses; i++)
                    {
                        NPC.SpawnNPC();
                    }
                }
                finally
                {
                    _inExtraSpawnPass = false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("WorldAndVisualPatches.SpawnNPC", ex);
            }
        }
    }
}

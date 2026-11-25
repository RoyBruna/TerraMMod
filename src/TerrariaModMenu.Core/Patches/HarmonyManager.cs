using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class HarmonyManager
    {
        private const string HarmonyId = "com.roybruna.terrammod";
        private static Harmony? _harmony;
        private static readonly List<string> InstalledPatches = new List<string>();
        private static readonly List<string> FailedPatches = new List<string>();

        public static IReadOnlyList<string> Installed => InstalledPatches;
        public static IReadOnlyList<string> Failed => FailedPatches;

        public static void InstallAll()
        {
            if (_harmony != null)
            {
                return;
            }

            InstalledPatches.Clear();
            FailedPatches.Clear();
            _harmony = new Harmony(HarmonyId);

            CraftingPatches.Install(_harmony);
            CombatPatches.Install(_harmony);
            ProjectilePatches.Install(_harmony);
            PlayerAndPotionPatches.Install(_harmony);
            WorldAndVisualPatches.Install(_harmony);

            ModLogger.Info($"Harmony installation complete: {InstalledPatches.Count} active, {FailedPatches.Count} failed.");
        }

        public static void SafePatch(
            Harmony harmony,
            string label,
            MethodBase? target,
            MethodInfo? prefix = null,
            MethodInfo? postfix = null)
        {
            try
            {
                if (target == null)
                {
                    FailedPatches.Add($"{label} (target method not found)");
                    ModLogger.Info($"WARNING: Harmony target not found for {label}");
                    return;
                }

                harmony.Patch(
                    target,
                    prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix: postfix != null ? new HarmonyMethod(postfix) : null);

                InstalledPatches.Add(label);
                ModLogger.Info($"Harmony patched: {label}");
            }
            catch (Exception ex)
            {
                FailedPatches.Add($"{label} ({ex.GetType().Name}: {ex.Message})");
                ModLogger.Error($"HarmonyManager.SafePatch({label})", ex);
            }
        }

        public static void UninstallAll()
        {
            try
            {
                if (_harmony != null)
                {
                    _harmony.UnpatchAll(HarmonyId);
                    _harmony = null;
                    InstalledPatches.Clear();
                    ModLogger.Info("All Harmony patches cleanly removed.");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("HarmonyManager.UninstallAll", ex);
            }
        }
    }
}

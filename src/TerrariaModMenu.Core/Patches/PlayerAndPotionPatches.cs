using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class PlayerAndPotionPatches
    {
        private static bool _reachModified;

        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "Player.UpdateEquips",
                AccessTools.Method(typeof(Player), nameof(Player.UpdateEquips), new[] { typeof(int) }),
                postfix: AccessTools.Method(typeof(PlayerAndPotionPatches), nameof(Postfix_UpdateEquips)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.BordersMovement",
                AccessTools.Method(typeof(Player), nameof(Player.BordersMovement), Type.EmptyTypes),
                postfix: AccessTools.Method(typeof(PlayerAndPotionPatches), nameof(Postfix_BordersMovement)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyPotionDelay",
                AccessTools.Method(typeof(Player), "ApplyPotionDelay", new[] { typeof(Item) }),
                prefix: AccessTools.Method(typeof(PlayerAndPotionPatches), nameof(Prefix_ApplyPotionDelay)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ApplyLifeAndOrMana",
                AccessTools.Method(typeof(Player), "ApplyLifeAndOrMana", new[] { typeof(Item) }),
                prefix: AccessTools.Method(typeof(PlayerAndPotionPatches), nameof(Prefix_ApplyLifeAndOrMana)));
        }

        private static void Postfix_UpdateEquips(Player __instance, int i)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || i != Main.myPlayer || Main.gameMenu)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;

                if (cfg.GodMode && !__instance.dead && !__instance.ghost)
                {
                    __instance.creativeGodMode = true;
                    if (__instance.statLifeMax2 > 0)
                    {
                        __instance.statLife = __instance.statLifeMax2;
                    }
                    __instance.immune = true;
                    __instance.immuneNoBlink = true;
                    if (__instance.immuneTime < 30)
                    {
                        __instance.immuneTime = 30;
                    }
                }

                if (cfg.Fullbright)
                {
                    Lighting.AddLight(__instance.Center, 2.5f, 2.5f, 2.5f);
                }

                if (cfg.InfiniteMana && !__instance.dead && !__instance.ghost)
                {
                    if (__instance.statManaMax2 > 0)
                    {
                        __instance.statMana = __instance.statManaMax2;
                    }
                    __instance.manaCost = 0f;
                }

                if (cfg.NoFallDamage) __instance.noFallDmg = true;
                if (cfg.NoKnockback) __instance.noKnockback = true;

                if (cfg.NoClip && !__instance.dead && !__instance.ghost)
                {
                    __instance.gravity = 0f;
                    __instance.maxFallSpeed = 0f;
                }

                if (cfg.InfiniteFlight)
                {
                    __instance.wingTime = Math.Max(__instance.wingTimeMax, 180f);
                    __instance.rocketTime = Math.Max(__instance.rocketTimeMax, 60);
                }

                if (cfg.InfiniteBreathAndLava)
                {
                    __instance.breath = __instance.breathMax;
                    __instance.lavaImmune = true;
                    __instance.fireWalk = true;
                    __instance.waterWalk = true;
                }

                if (cfg.ForceAutoReuseAll) __instance.autoReuseAllWeapons = true;
                if (cfg.VisionSpelunker) __instance.findTreasure = true;
                if (cfg.VisionHunter) __instance.detectCreature = true;
                if (cfg.VisionDangerSense) __instance.dangerSense = true;
                if (cfg.VisionNightVision) __instance.nightVision = true;

                if (cfg.AutoPurgeDebuffs)
                {
                    GameActions.PurgeDebuffsImmediate(__instance);
                }

                if (cfg.NoPotionCooldown)
                {
                    __instance.potionDelay = 0;
                    __instance.manaPotionDelay = 0;
                    __instance.ClearBuff(BuffID.PotionSickness);
                    __instance.ClearBuff(BuffID.ManaSickness);
                }

                if (cfg.InfiniteBuffDuration && __instance.buffType != null && __instance.buffTime != null)
                {
                    int len = Math.Min(__instance.buffType.Length, __instance.buffTime.Length);
                    for (int b = 0; b < len; b++)
                    {
                        int bType = __instance.buffType[b];
                        if (bType > 0 && bType < Main.debuff.Length && !Main.debuff[bType] && __instance.buffTime[b] > 1 && __instance.buffTime[b] < 36000)
                        {
                            __instance.buffTime[b] = 36000;
                        }
                    }
                }

                if (cfg.OpPotionsEnabled && cfg.OpPotionMultiplier > 1.0f)
                {
                    ApplyPotionMultipliers(__instance, cfg);
                }

                if (cfg.MoveSpeedMultiplier > 1.01f)
                {
                    float m = cfg.MoveSpeedMultiplier;
                    __instance.moveSpeed *= m;
                    __instance.maxRunSpeed *= m;
                    __instance.accRunSpeed *= m;
                    __instance.runAcceleration *= m;
                }

                if (cfg.JumpBoostBonus > 0.01f) __instance.jumpSpeedBoost += cfg.JumpBoostBonus;
                if (cfg.BonusDefense > 0) __instance.statDefense += cfg.BonusDefense;
                if (cfg.BonusLifeRegen > 0) __instance.lifeRegen += cfg.BonusLifeRegen;

                if (cfg.ReachBonusTiles > 0)
                {
                    Player.tileRangeX = Player.DefaultTileRangeX + cfg.ReachBonusTiles;
                    Player.tileRangeY = Player.DefaultTileRangeY + cfg.ReachBonusTiles;
                    __instance.blockRange += cfg.ReachBonusTiles;
                    _reachModified = true;
                }
                else if (_reachModified)
                {
                    Player.tileRangeX = Player.DefaultTileRangeX;
                    Player.tileRangeY = Player.DefaultTileRangeY;
                    _reachModified = false;
                }

                if (cfg.MiningSpeedMultiplier > 1.01f)
                {
                    __instance.pickSpeed = Math.Max(0.05f, __instance.pickSpeed / cfg.MiningSpeedMultiplier);
                    __instance.tileSpeed *= cfg.MiningSpeedMultiplier;
                    __instance.wallSpeed *= cfg.MiningSpeedMultiplier;
                }

                if (cfg.GlobalDamageMultiplier > 1.01f)
                {
                    float dm = cfg.GlobalDamageMultiplier;
                    __instance.meleeDamage *= dm;
                    __instance.rangedDamage *= dm;
                    __instance.magicDamage *= dm;
                    __instance.minionDamage *= dm;
                }

                if (cfg.BonusCritChance > 0)
                {
                    __instance.meleeCrit += cfg.BonusCritChance;
                    __instance.rangedCrit += cfg.BonusCritChance;
                    __instance.magicCrit += cfg.BonusCritChance;
                }

                if (cfg.FishingPowerBonus > 0) __instance.fishingSkill += cfg.FishingPowerBonus;
                if (cfg.FishingNeverBreakLine) __instance.accFishingLine = true;
                if (cfg.FishingLavaEnabled) __instance.accLavaFishing = true;
                if (cfg.FishingInfiniteBait) __instance.accTackleBox = true;
                if (cfg.FishingSonarAndCrate)
                {
                    __instance.sonarPotion = true;
                    __instance.cratePotion = true;
                }

                if (cfg.BonusMaxMinions > 0) __instance.maxMinions += cfg.BonusMaxMinions;
                if (cfg.BonusMaxSentries > 0) __instance.maxTurrets += cfg.BonusMaxSentries;
                if (cfg.SummonDamageMultiplier > 1.01f) __instance.minionDamage *= cfg.SummonDamageMultiplier;
                if (cfg.BonusMinionKnockback > 0.01f) __instance.minionKB += cfg.BonusMinionKnockback;
                if (cfg.WhipRangeMultiplier > 1.01f) __instance.whipRangeMultiplier *= cfg.WhipRangeMultiplier;
                if (cfg.WhipSpeedMultiplier > 1.01f)
                {
                    __instance.whipUseTimeMultiplier = Math.Max(0.1f, __instance.whipUseTimeMultiplier / cfg.WhipSpeedMultiplier);
                }
                if (cfg.BonusMaxWhipTags > 0) __instance.maxTagEffects += cfg.BonusMaxWhipTags;
                if (cfg.BonusWhipTagDuration > 0.01f) __instance.tagEffectDuration += cfg.BonusWhipTagDuration;
            }
            catch (Exception ex)
            {
                ModLogger.Error("PlayerAndPotionPatches.UpdateEquips", ex);
            }
        }

        private static void ApplyPotionMultipliers(Player player, ModConfig cfg)
        {
            float extra = cfg.OpPotionMultiplier - 1.0f;

            if (cfg.OpIronskin && player.FindBuffIndex(BuffID.Ironskin) >= 0)
            {
                player.statDefense += (int)(8f * extra);
            }

            if (cfg.OpEndurance && player.FindBuffIndex(BuffID.Endurance) >= 0)
            {
                player.endurance = Math.Min(0.95f, player.endurance + 0.10f * extra);
            }

            if (cfg.OpSwiftness && player.FindBuffIndex(BuffID.Swiftness) >= 0)
            {
                float boost = 1f + 0.25f * extra;
                player.moveSpeed *= boost;
                player.maxRunSpeed *= boost;
                player.accRunSpeed *= boost;
            }

            if (cfg.OpDamagePotions)
            {
                if (player.FindBuffIndex(BuffID.Wrath) >= 0)
                {
                    float d = 0.10f * extra;
                    player.meleeDamage += d;
                    player.rangedDamage += d;
                    player.magicDamage += d;
                    player.minionDamage += d;
                }
                if (player.FindBuffIndex(BuffID.Rage) >= 0)
                {
                    int c = (int)(10f * extra);
                    player.meleeCrit += c;
                    player.rangedCrit += c;
                    player.magicCrit += c;
                }
                if (player.FindBuffIndex(BuffID.MagicPower) >= 0)
                {
                    player.magicDamage += 0.20f * extra;
                }
                if (player.FindBuffIndex(BuffID.Archery) >= 0)
                {
                    player.rangedDamage += 0.20f * extra;
                }
            }

            if (cfg.OpRegeneration && player.FindBuffIndex(BuffID.Regeneration) >= 0)
            {
                player.lifeRegen += (int)(12f * extra);
            }

            if (cfg.OpLifeforce && player.FindBuffIndex(BuffID.Lifeforce) >= 0)
            {
                player.statLifeMax2 += (int)(player.statLifeMax * 0.20f * extra);
            }

            if (cfg.OpMiningBuilder)
            {
                if (player.FindBuffIndex(BuffID.Mining) >= 0)
                {
                    player.pickSpeed = Math.Max(0.05f, player.pickSpeed / (1f + 0.35f * extra));
                }
                if (player.FindBuffIndex(BuffID.Builder) >= 0)
                {
                    player.tileSpeed *= 1f + 0.35f * extra;
                    player.wallSpeed *= 1f + 0.35f * extra;
                    player.blockRange += (int)(2f * extra);
                }
            }
        }

        private static void Postfix_BordersMovement(Player __instance)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || Main.gameMenu || __instance.dead || __instance.ghost)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                if (cfg.NoClip && !Main.drawingPlayerChat)
                {
                    Vector2 move = Vector2.Zero;
                    if (__instance.controlUp) move.Y -= 1f;
                    if (__instance.controlDown) move.Y += 1f;
                    if (__instance.controlLeft) move.X -= 1f;
                    if (__instance.controlRight) move.X += 1f;

                    if (move != Vector2.Zero)
                    {
                        move.Normalize();
                        Vector2 next = __instance.position + move * cfg.NoClipSpeed;
                        float minX = Main.leftWorld + 640f;
                        float maxX = Main.rightWorld - 640f - __instance.width;
                        float minY = Main.topWorld + 640f;
                        float maxY = Main.bottomWorld - 640f - __instance.height;
                        next.X = Math.Max(minX, Math.Min(maxX, next.X));
                        next.Y = Math.Max(minY, Math.Min(maxY, next.Y));
                        __instance.position = next;
                    }

                    __instance.velocity = Vector2.Zero;
                    __instance.fallStart = (int)(__instance.position.Y / 16f);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("PlayerAndPotionPatches.BordersMovement", ex);
            }
        }

        private static bool Prefix_ApplyPotionDelay(Player __instance)
        {
            try
            {
                if (ModRuntime.IsFullModeActive && __instance != null && __instance.whoAmI == Main.myPlayer && ModConfig.Instance.NoPotionCooldown)
                {
                    __instance.potionDelay = 0;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("PlayerAndPotionPatches.ApplyPotionDelay", ex);
            }
            return true;
        }

        private static bool Prefix_ApplyLifeAndOrMana(Player __instance, Item item)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer || item == null || !ModConfig.Instance.SuperHealAndManaPotions)
                {
                    return true;
                }

                if (item.healLife > 0)
                {
                    int healAmount = Math.Max(item.healLife, __instance.statLifeMax2 - __instance.statLife);
                    __instance.statLife = __instance.statLifeMax2;
                    if (healAmount > 0)
                    {
                        __instance.HealEffect(healAmount, true);
                    }
                    __instance.SetImmuneTimeForAllTypes(90);
                }

                if (item.healMana > 0)
                {
                    int manaAmount = Math.Max(item.healMana, __instance.statManaMax2 - __instance.statMana);
                    __instance.statMana = __instance.statManaMax2;
                    if (manaAmount > 0)
                    {
                        __instance.ManaEffect(manaAmount);
                    }
                    __instance.manaPotionDelay = 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("PlayerAndPotionPatches.ApplyLifeAndOrMana", ex);
                return true;
            }
        }
    }
}

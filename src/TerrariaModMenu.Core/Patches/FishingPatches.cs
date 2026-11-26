using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal readonly struct FishingTelemetry
    {
        public bool InWorld { get; }
        public int FinalPower { get; }
        public string PoleName { get; }
        public string BaitName { get; }
        public int ActiveBobbers { get; }
        public int BitingBobbers { get; }
        public int QuestFishId { get; }
        public string QuestFishName { get; }
        public bool QuestFinishedToday { get; }
        public int QuestsCompletedTotal { get; }

        public FishingTelemetry(
            bool inWorld,
            int finalPower,
            string poleName,
            string baitName,
            int activeBobbers,
            int bitingBobbers,
            int questFishId,
            string questFishName,
            bool questFinishedToday,
            int questsCompletedTotal)
        {
            InWorld = inWorld;
            FinalPower = finalPower;
            PoleName = poleName;
            BaitName = baitName;
            ActiveBobbers = activeBobbers;
            BitingBobbers = bitingBobbers;
            QuestFishId = questFishId;
            QuestFishName = questFishName;
            QuestFinishedToday = questFinishedToday;
            QuestsCompletedTotal = questsCompletedTotal;
        }
    }

    internal static class FishingPatches
    {
        private static bool _spawningExtraBobbers;
        private static Vector2 _lastCastVelocity;
        private static int _lastCastBobberType;
        private static bool _hasLastCast;
        private static bool _sawActiveBobbers;
        private static int _recastDelayTicks;

        private static readonly MethodInfo? ConsumeBaitMethod =
            AccessTools.Method(typeof(Player), "ItemCheck_CheckFishingBobber_ConsumeBait", new[] { typeof(Projectile), typeof(int).MakeByRefType() });

        private static readonly MethodInfo? PullBobberMethod =
            AccessTools.Method(typeof(Player), "ItemCheck_CheckFishingBobber_PullBobber", new[] { typeof(Projectile), typeof(int) });

        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "Player.Fishing_GetBait",
                AccessTools.Method(typeof(Player), "Fishing_GetBait", new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType() }),
                postfix: AccessTools.Method(typeof(FishingPatches), nameof(Postfix_FishingGetBait)));

            HarmonyManager.SafePatch(
                harmony,
                "Player.ConsumeBait",
                ConsumeBaitMethod,
                prefix: AccessTools.Method(typeof(FishingPatches), nameof(Prefix_ConsumeBait)));

            HarmonyManager.SafePatch(
                harmony,
                "Projectile.GetFishingPondState",
                AccessTools.Method(typeof(Projectile), "GetFishingPondState", new[]
                {
                    typeof(int), typeof(int),
                    typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(),
                    typeof(int).MakeByRefType(), typeof(int).MakeByRefType()
                }),
                postfix: AccessTools.Method(typeof(FishingPatches), nameof(Postfix_GetFishingPondState)));

            HarmonyManager.SafePatch(
                harmony,
                "Projectile.FishingCheck_RollDropLevels",
                AccessTools.Method(typeof(Projectile), "FishingCheck_RollDropLevels", new[]
                {
                    typeof(int),
                    typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(),
                    typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(),
                    typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()
                }),
                postfix: AccessTools.Method(typeof(FishingPatches), nameof(Postfix_RollDropLevels)));

            HarmonyManager.SafePatch(
                harmony,
                "Projectile.FishingCheck_ProbeForQuestFish",
                AccessTools.Method(typeof(Projectile), "FishingCheck_ProbeForQuestFish", new[] { typeof(FishingAttempt).MakeByRefType() }),
                postfix: AccessTools.Method(typeof(FishingPatches), nameof(Postfix_ProbeForQuestFish)));
        }

        public static void HandleBobberSpawn(
            IEntitySource spawnSource,
            float x,
            float y,
            float speedX,
            float speedY,
            int type,
            int damage,
            float knockBack,
            int owner,
            float ai0,
            float ai1,
            float ai2,
            NewProjectileModifier modifier)
        {
            if (_spawningExtraBobbers)
            {
                return;
            }

            _lastCastVelocity = new Vector2(speedX, speedY);
            _lastCastBobberType = type;
            _hasLastCast = true;
            _sawActiveBobbers = true;
            _recastDelayTicks = 0;

            ModConfig cfg = ModConfig.Instance;
            if (!cfg.FishingMultiBobberEnabled || cfg.FishingBobberCount <= 1)
            {
                return;
            }

            int desired = Math.Max(2, Math.Min(50, cfg.FishingBobberCount));
            int activeNow = CountActiveProjectiles();
            int budget = (Main.maxProjectiles - 120) - activeNow;
            if (budget <= 0)
            {
                return;
            }

            int extraCount = Math.Min(desired - 1, budget);
            int resolvedOwner = owner == -1 ? Main.myPlayer : owner;
            double spreadFactor = Math.Max(0.05, Math.Min(1.0, cfg.FishingCastSpreadPercent / 100.0));
            double baseAngle = Math.Atan2(speedY, speedX);
            double baseSpeed = Math.Sqrt(speedX * speedX + speedY * speedY);
            if (baseSpeed < 1.0)
            {
                baseSpeed = 8.0;
            }

            _spawningExtraBobbers = true;
            try
            {
                for (int k = 0; k < extraCount; k++)
                {
                    double norm = extraCount == 1 ? 0.0 : ((double)k / (extraCount - 1) - 0.5) * 2.0;
                    double angleOffset = norm * (0.42 * spreadFactor);
                    double speedScale = 1.0 + norm * (0.28 * spreadFactor) + ((k % 3) - 1) * (0.06 * spreadFactor);
                    double angle = baseAngle + angleOffset;
                    float vx = (float)(Math.Cos(angle) * baseSpeed * speedScale);
                    float vy = (float)(Math.Sin(angle) * baseSpeed * speedScale);

                    Projectile.NewProjectile(
                        spawnSource, x, y, vx, vy,
                        type, damage, knockBack, resolvedOwner,
                        ai0, ai1, ai2, modifier);
                }
            }
            finally
            {
                _spawningExtraBobbers = false;
            }
        }

        public static void TickFishing()
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || Main.gameMenu || Main.projectile == null)
                {
                    return;
                }

                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null || player.dead || player.ghost)
                {
                    _sawActiveBobbers = false;
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                bool instantBite = cfg.FishingInstantBite;
                bool autoReel = cfg.FishingAutoReel;
                bool autoRecast = cfg.FishingAutoRecast;

                if (!instantBite && !autoReel && !autoRecast)
                {
                    return;
                }

                int activeBobbers = 0;
                Projectile[] projectiles = Main.projectile;
                int limit = Math.Min(Main.maxProjectiles, projectiles.Length);

                for (int i = 0; i < limit; i++)
                {
                    Projectile? p = projectiles[i];
                    if (p == null || !p.active || !p.bobber || p.owner != Main.myPlayer)
                    {
                        continue;
                    }

                    activeBobbers++;

                    if (p.ai[0] != 0f)
                    {
                        continue;
                    }

                    if (instantBite && p.ai[1] == 0f && p.wet && p.localAI[1] < 661f)
                    {
                        p.localAI[1] = 661f;
                    }

                    if (autoReel && p.ai[1] < 0f && p.localAI[1] != 0f)
                    {
                        if (MatchesAutoReelFilter(p, cfg.FishingAutoReelFilter))
                        {
                            ReelSingleBobber(player, p);
                        }
                        else
                        {
                            p.ai[1] = 0f;
                            p.localAI[1] = 0f;
                            p.netUpdate = true;
                        }
                    }
                }

                if (activeBobbers > 0)
                {
                    _sawActiveBobbers = true;
                    _recastDelayTicks = 0;
                }
                else if (autoRecast && _sawActiveBobbers && _hasLastCast)
                {
                    Item? held = player.HeldItem;
                    if (held == null || held.IsAir || held.fishingPole <= 0 || player.CCed || player.noItems || player.pulley)
                    {
                        _sawActiveBobbers = false;
                        _recastDelayTicks = 0;
                        return;
                    }

                    _recastDelayTicks++;
                    if (_recastDelayTicks >= 12)
                    {
                        _recastDelayTicks = 0;
                        int bobberType = player.overrideFishingBobber > -1 ? player.overrideFishingBobber : held.shoot;
                        if (bobberType <= 0)
                        {
                            bobberType = _lastCastBobberType;
                        }

                        if (bobberType > 0)
                        {
                            Vector2 origin = player.MountedCenter;
                            IEntitySource source = new EntitySource_ItemUse(player, held);
                            Projectile.NewProjectile(
                                source,
                                origin.X,
                                origin.Y,
                                _lastCastVelocity.X,
                                _lastCastVelocity.Y,
                                bobberType,
                                0,
                                0f,
                                Main.myPlayer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.TickFishing", ex);
            }
        }

        public static void ReelAllActiveBobbersNow()
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || Main.projectile == null)
                {
                    return;
                }

                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return;
                }

                Projectile[] projectiles = Main.projectile;
                int limit = Math.Min(Main.maxProjectiles, projectiles.Length);
                int reeled = 0;

                for (int i = 0; i < limit; i++)
                {
                    Projectile? p = projectiles[i];
                    if (p == null || !p.active || !p.bobber || p.owner != Main.myPlayer || p.ai[0] != 0f)
                    {
                        continue;
                    }

                    ReelSingleBobber(player, p);
                    reeled++;
                }

                if (reeled > 0)
                {
                    Main.NewText($"[ModMenu] Reeled in {reeled} bobber(s)", 56, 189, 248);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.ReelAllActiveBobbersNow", ex);
            }
        }

        private static bool MatchesAutoReelFilter(Projectile bobber, int filter)
        {
            int raw = (int)bobber.localAI[1];
            if (filter == 0)
            {
                return true;
            }

            if (raw < 0)
            {
                return false;
            }

            if (filter == 1)
            {
                return raw > 0;
            }

            bool isCrate = raw > 0 && raw < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[raw];
            if (filter == 2)
            {
                return isCrate;
            }

            bool isQuest = raw > 0 && raw < ItemID.Sets.IsQuestFish.Length && ItemID.Sets.IsQuestFish[raw];
            return isCrate || isQuest;
        }

        private static void ReelSingleBobber(Player player, Projectile bobber)
        {
            bobber.ai[0] = 1f;
            if (bobber.wet && bobber.velocity.Y > -10f)
            {
                bobber.velocity.Y = -10f;
            }
            bobber.netUpdate2 = true;

            if (bobber.ai[1] < 0f && bobber.localAI[1] != 0f && ConsumeBaitMethod != null && PullBobberMethod != null)
            {
                object?[] consumeArgs = { bobber, 0 };
                object? consumedObj = ConsumeBaitMethod.Invoke(player, consumeArgs);
                if (consumedObj is bool consumed && consumed)
                {
                    int baitTypeUsed = consumeArgs[1] is int b ? b : 0;
                    PullBobberMethod.Invoke(player, new object?[] { bobber, baitTypeUsed });
                }
            }
        }

        public static FishingTelemetry CaptureTelemetry()
        {
            try
            {
                if (!GameState.TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return default;
                }

                PlayerFishingConditions cond = player.GetFishingConditions();
                string poleName = cond.PoleItemType > 0 ? Lang.GetItemNameValue(cond.PoleItemType) : "None";
                string baitName = cond.BaitItemType > 0 ? Lang.GetItemNameValue(cond.BaitItemType) : "None";

                int active = 0;
                int biting = 0;
                Projectile[]? arr = Main.projectile;
                if (arr != null)
                {
                    int limit = Math.Min(Main.maxProjectiles, arr.Length);
                    for (int i = 0; i < limit; i++)
                    {
                        Projectile? p = arr[i];
                        if (p != null && p.active && p.bobber && p.owner == Main.myPlayer)
                        {
                            active++;
                            if (p.ai[0] == 0f && p.ai[1] < 0f && p.localAI[1] != 0f)
                            {
                                biting++;
                            }
                        }
                    }
                }

                int questFishId = 0;
                string questFishName = "None";
                if (Main.anglerQuestItemNetIDs != null &&
                    Main.anglerQuest >= 0 &&
                    Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
                {
                    questFishId = Main.anglerQuestItemNetIDs[Main.anglerQuest];
                    string qName = Lang.GetItemNameValue(questFishId);
                    if (!string.IsNullOrWhiteSpace(qName))
                    {
                        questFishName = qName;
                    }
                }

                return new FishingTelemetry(
                    true,
                    cond.FinalFishingLevel,
                    poleName,
                    baitName,
                    active,
                    biting,
                    questFishId,
                    questFishName,
                    Main.anglerQuestFinished,
                    player.anglerQuestsFinished);
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.CaptureTelemetry", ex);
                return default;
            }
        }

        private static int CountActiveProjectiles()
        {
            int count = 0;
            Projectile[]? arr = Main.projectile;
            if (arr == null)
            {
                return 0;
            }

            int limit = Math.Min(Main.maxProjectiles, arr.Length);
            for (int i = 0; i < limit; i++)
            {
                if (arr[i] != null && arr[i].active)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Postfix_FishingGetBait(Player __instance, ref int baitPower, ref int baitType)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer)
                {
                    return;
                }

                if (ModConfig.Instance.FishingVirtualBait && (baitPower <= 0 || baitType <= 0))
                {
                    baitPower = 50;
                    baitType = ItemID.MasterBait;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.FishingGetBait", ex);
            }
        }

        private static bool Prefix_ConsumeBait(Player __instance, Projectile bobber, ref int baitTypeUsed, ref bool __result)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.whoAmI != Main.myPlayer)
                {
                    return true;
                }

                ModConfig cfg = ModConfig.Instance;
                if (!cfg.FishingInfiniteBait && !cfg.FishingVirtualBait)
                {
                    return true;
                }

                int recorded = bobber != null ? (int)bobber.localAI[2] : 0;
                if (cfg.FishingInfiniteBait || recorded == ItemID.MasterBait || recorded <= 0)
                {
                    baitTypeUsed = recorded > 0 ? recorded : ItemID.MasterBait;
                    __result = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.ConsumeBait", ex);
            }
            return true;
        }

        private static void Postfix_GetFishingPondState(ref int numWaters, ref int chumCount)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || !ModConfig.Instance.FishingIgnorePondSize)
                {
                    return;
                }

                if (numWaters > 0 && numWaters < 300)
                {
                    numWaters = 300;
                }
                if (chumCount < 3)
                {
                    chumCount = 3;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.GetFishingPondState", ex);
            }
        }

        private static void Postfix_RollDropLevels(
            Projectile __instance,
            ref bool rare,
            ref bool veryrare,
            ref bool legendary,
            ref bool crate)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.owner != Main.myPlayer)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                if (cfg.FishingGuaranteedCrate)
                {
                    crate = true;
                }
                else if (cfg.FishingCrateChance > 0 && Main.rand.Next(100) < cfg.FishingCrateChance)
                {
                    crate = true;
                }

                if (cfg.FishingBoostRare)
                {
                    rare = true;
                    veryrare = true;
                    legendary = true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.RollDropLevels", ex);
            }
        }

        private static void Postfix_ProbeForQuestFish(Projectile __instance, ref FishingAttempt fisher)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || __instance == null || __instance.owner != Main.myPlayer)
                {
                    return;
                }

                if (ModConfig.Instance.FishingAllowMultipleQuestFish &&
                    Main.anglerQuestItemNetIDs != null &&
                    Main.anglerQuest >= 0 &&
                    Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
                {
                    fisher.questFish = Main.anglerQuestItemNetIDs[Main.anglerQuest];
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FishingPatches.ProbeForQuestFish", ex);
            }
        }
    }
}

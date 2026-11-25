using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TerrariaModMenu.Core.Core;

namespace TerrariaModMenu.Core.Patches
{
    internal static class ProjectilePatches
    {
        private static bool _spawningExtra;

        public static void Install(Harmony harmony)
        {
            HarmonyManager.SafePatch(
                harmony,
                "Projectile.NewProjectile",
                AccessTools.Method(typeof(Projectile), nameof(Projectile.NewProjectile), new[]
                {
                    typeof(IEntitySource),
                    typeof(float), typeof(float), typeof(float), typeof(float),
                    typeof(int), typeof(int), typeof(float), typeof(int),
                    typeof(float), typeof(float), typeof(float),
                    typeof(NewProjectileModifier)
                }),
                postfix: AccessTools.Method(typeof(ProjectilePatches), nameof(Postfix_NewProjectile)));

            HarmonyManager.SafePatch(
                harmony,
                "Projectile.AI",
                AccessTools.Method(typeof(Projectile), nameof(Projectile.AI), Type.EmptyTypes),
                postfix: AccessTools.Method(typeof(ProjectilePatches), nameof(Postfix_ProjectileAI)));
        }

        private static bool IsEligibleCombatProjectile(Projectile? p)
        {
            if (p == null || !p.active || !p.friendly || p.hostile || p.owner != Main.myPlayer || p.damage <= 0)
            {
                return false;
            }

            if (p.minion || p.sentry || p.bobber)
            {
                return false;
            }

            if (p.type >= 0 && Main.projHook != null && p.type < Main.projHook.Length && Main.projHook[p.type])
            {
                return false;
            }

            if (p.type >= 0 && Main.projPet != null && p.type < Main.projPet.Length && Main.projPet[p.type])
            {
                return false;
            }

            switch (p.aiStyle)
            {
                case 7:
                case 15:
                case 19:
                case 75:
                case 84:
                case 99:
                case 161:
                case 165:
                    return false;
                default:
                    return true;
            }
        }

        private static void ApplySpawnModifiers(Projectile p, ModConfig cfg)
        {
            if (cfg.ProjectileSpeedMultiplier > 1.01f)
            {
                p.velocity *= cfg.ProjectileSpeedMultiplier;
            }

            if (cfg.ProjectileNoTileCollide)
            {
                p.tileCollide = false;
            }

            if (cfg.ProjectileInfinitePierce)
            {
                if (p.penetrate > 0 && p.penetrate < 999)
                {
                    p.penetrate = 999;
                }
                p.usesLocalNPCImmunity = true;
                if (p.localNPCHitCooldown <= 0 || p.localNPCHitCooldown > 8)
                {
                    p.localNPCHitCooldown = 8;
                }
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

            for (int i = 0; i < Main.maxProjectiles && i < arr.Length; i++)
            {
                if (arr[i] != null && arr[i].active)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Postfix_NewProjectile(
            int __result,
            IEntitySource spawnSource,
            float X,
            float Y,
            float SpeedX,
            float SpeedY,
            int Type,
            int Damage,
            float KnockBack,
            int Owner,
            float ai0,
            float ai1,
            float ai2,
            NewProjectileModifier modifier)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || _spawningExtra || __result < 0 || __result >= Main.maxProjectiles)
                {
                    return;
                }

                Projectile? orig = Main.projectile?[__result];
                if (!IsEligibleCombatProjectile(orig) || orig == null)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                ApplySpawnModifiers(orig, cfg);

                if (!cfg.ProjectileMultishotEnabled || cfg.ProjectileMultishotCount <= 1)
                {
                    return;
                }

                float speedSq = SpeedX * SpeedX + SpeedY * SpeedY;
                if (speedSq < 0.04f)
                {
                    return;
                }

                int totalShots = Math.Max(2, Math.Min(10, cfg.ProjectileMultishotCount));
                int activeNow = CountActiveProjectiles();
                int budget = (Main.maxProjectiles - 100) - activeNow;
                if (budget <= 0)
                {
                    return;
                }

                int extraCount = Math.Min(totalShots - 1, budget);
                double spreadRad = cfg.ProjectileMultishotSpreadDeg * (Math.PI / 180.0);
                double baseAngle = Math.Atan2(SpeedY, SpeedX);
                double speed = Math.Sqrt(speedSq);
                int resolvedOwner = Owner == -1 ? Main.myPlayer : Owner;

                _spawningExtra = true;
                try
                {
                    for (int k = 0; k < extraCount; k++)
                    {
                        double t = extraCount == 1 ? 0.5 : (double)k / (extraCount - 1);
                        double offset = (t - 0.5) * spreadRad;
                        if (Math.Abs(offset) < 0.015)
                        {
                            offset = (k % 2 == 0 ? 1.0 : -1.0) * (spreadRad * 0.25);
                        }

                        double angle = baseAngle + offset;
                        float vx = (float)(Math.Cos(angle) * speed);
                        float vy = (float)(Math.Sin(angle) * speed);

                        int idx = Projectile.NewProjectile(
                            spawnSource, X, Y, vx, vy,
                            Type, Damage, KnockBack, resolvedOwner,
                            ai0, ai1, ai2, modifier);

                        if (idx >= 0 && idx < Main.maxProjectiles && Main.projectile != null && Main.projectile[idx] != null)
                        {
                            ApplySpawnModifiers(Main.projectile[idx], cfg);
                        }
                    }
                }
                finally
                {
                    _spawningExtra = false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ProjectilePatches.NewProjectile", ex);
            }
        }

        private static void Postfix_ProjectileAI(Projectile __instance)
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || Main.gameMenu || !IsEligibleCombatProjectile(__instance))
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;

                if (cfg.ProjectileNoTileCollide)
                {
                    __instance.tileCollide = false;
                }

                if (!cfg.ProjectileUniversalHoming || Main.npc == null)
                {
                    return;
                }

                float maxDistSq = cfg.ProjectileHomingRange * cfg.ProjectileHomingRange;
                Vector2 origin = __instance.Center;
                NPC? bestTarget = null;
                float bestDistSq = maxDistSq;

                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC? npc = Main.npc[i];
                    if (npc == null || !npc.CanBeChasedBy(__instance, false))
                    {
                        continue;
                    }

                    float dSq = Vector2.DistanceSquared(origin, npc.Center);
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestTarget = npc;
                    }
                }

                if (bestTarget == null)
                {
                    return;
                }

                Vector2 toTarget = bestTarget.Center - origin;
                float dist = toTarget.Length();
                if (dist < 1f)
                {
                    return;
                }

                float currentSpeed = __instance.velocity.Length();
                float targetSpeed = Math.Max(currentSpeed, 10f);
                Vector2 desiredVel = (toTarget / dist) * targetSpeed;
                float turn = Math.Max(0.05f, Math.Min(0.6f, cfg.ProjectileHomingTurn));

                Vector2 steered = Vector2.Lerp(__instance.velocity, desiredVel, turn);
                float steeredLen = steered.Length();
                if (steeredLen > 0.01f)
                {
                    __instance.velocity = steered * (targetSpeed / steeredLen);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ProjectilePatches.ProjectileAI", ex);
            }
        }
    }
}

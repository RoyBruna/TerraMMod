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

                ModConfig cfg = ModConfig.Instance;
                if (!cfg.ProjectileMultishotEnabled &&
                    !cfg.ProjectileNoTileCollide &&
                    !cfg.ProjectileInfinitePierce &&
                    cfg.ProjectileSpeedMultiplier <= 1.01f)
                {
                    return;
                }

                Projectile? orig = Main.projectile?[__result];
                if (!IsEligibleCombatProjectile(orig) || orig == null)
                {
                    return;
                }

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

        public static void TickActiveProjectiles()
        {
            try
            {
                if (!ModRuntime.IsFullModeActive || Main.gameMenu || Main.projectile == null)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                if (!cfg.ProjectileNoTileCollide && !cfg.ProjectileUniversalHoming)
                {
                    return;
                }

                Projectile[] projectiles = Main.projectile;
                int limit = Math.Min(Main.maxProjectiles, projectiles.Length);
                bool homing = cfg.ProjectileUniversalHoming && Main.npc != null;
                float maxDistSq = cfg.ProjectileHomingRange * cfg.ProjectileHomingRange;
                float turn = Math.Max(0.05f, Math.Min(0.6f, cfg.ProjectileHomingTurn));

                for (int pIdx = 0; pIdx < limit; pIdx++)
                {
                    Projectile? proj = projectiles[pIdx];
                    if (!IsEligibleCombatProjectile(proj) || proj == null)
                    {
                        continue;
                    }

                    if (cfg.ProjectileNoTileCollide)
                    {
                        proj.tileCollide = false;
                    }

                    if (!homing)
                    {
                        continue;
                    }

                    Vector2 origin = proj.Center;
                    NPC? bestTarget = null;
                    float bestDistSq = maxDistSq;
                    NPC[] npcs = Main.npc!;

                    for (int i = 0; i < npcs.Length; i++)
                    {
                        NPC? npc = npcs[i];
                        if (npc == null || !npc.CanBeChasedBy(proj, false))
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
                        continue;
                    }

                    Vector2 toTarget = bestTarget.Center - origin;
                    float dist = toTarget.Length();
                    if (dist < 1f)
                    {
                        continue;
                    }

                    float currentSpeed = proj.velocity.Length();
                    float targetSpeed = Math.Max(currentSpeed, 10f);
                    Vector2 desiredVel = (toTarget / dist) * targetSpeed;

                    Vector2 steered = Vector2.Lerp(proj.velocity, desiredVel, turn);
                    float steeredLen = steered.Length();
                    if (steeredLen > 0.01f)
                    {
                        proj.velocity = steered * (targetSpeed / steeredLen);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ProjectilePatches.TickActiveProjectiles", ex);
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Testing;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.Core
{
    internal static class CrashReporter
    {
        private static readonly object Sync = new object();
        private static readonly ConcurrentDictionary<string, long> FirstChanceThrottle = new ConcurrentDictionary<string, long>();
        private static string _baseDir = string.Empty;
        private static bool _installed;
        private static int _firstChanceCount;
        [ThreadStatic]
        private static bool _inFirstChance;

        public static void Install(string baseDirectory)
        {
            lock (Sync)
            {
                _baseDir = string.IsNullOrWhiteSpace(baseDirectory)
                    ? (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Path.GetTempPath())
                    : baseDirectory;

                if (_installed)
                {
                    return;
                }

                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                {
                    WriteReport("AppDomain.UnhandledException", args.ExceptionObject as Exception, args.IsTerminating);
                };

                TaskScheduler.UnobservedTaskException += (_, args) =>
                {
                    WriteReport("TaskScheduler.UnobservedTaskException", args.Exception, false);
                    args.SetObserved();
                };

                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

                _installed = true;
                ModLogger.Info($"CrashReporter initialized ({_baseDir}).");
            }
        }

        private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (_inFirstChance || _firstChanceCount >= 25)
            {
                return;
            }

            Exception? ex = args.Exception;
            if (ex == null || !IsTrackedException(ex))
            {
                return;
            }

            _inFirstChance = true;
            try
            {
                var st = new StackTrace(1, false);
                string trace = st.ToString();
                if (trace.IndexOf("Terraria.Player", StringComparison.Ordinal) < 0 &&
                    trace.IndexOf("Terraria.Projectile", StringComparison.Ordinal) < 0 &&
                    trace.IndexOf("TerrariaModMenu", StringComparison.Ordinal) < 0 &&
                    trace.IndexOf("Harmony", StringComparison.Ordinal) < 0 &&
                    trace.IndexOf("_Patch", StringComparison.Ordinal) < 0)
                {
                    return;
                }

                MethodBase? frameMethod = st.FrameCount > 0 ? st.GetFrame(0)?.GetMethod() : null;
                string site = frameMethod != null
                    ? $"{frameMethod.DeclaringType?.FullName}.{frameMethod.Name}"
                    : "unknown";
                string key = $"{ex.GetType().FullName}|{site}";
                long now = Environment.TickCount;
                if (FirstChanceThrottle.TryGetValue(key, out long prev) && (now - prev) < 15000)
                {
                    return;
                }

                FirstChanceThrottle[key] = now;
                Interlocked.Increment(ref _firstChanceCount);
                ModLogger.Info($"FIRST-CHANCE {ex.GetType().FullName}: {ex.Message} @ {site}\n{trace}");
            }
            catch
            {
            }
            finally
            {
                _inFirstChance = false;
            }
        }

        private static bool IsTrackedException(Exception ex)
        {
            return ex is NullReferenceException ||
                   ex is InvalidProgramException ||
                   ex is IndexOutOfRangeException ||
                   ex is ArgumentOutOfRangeException ||
                   ex is MissingMethodException ||
                   ex is MissingFieldException ||
                   ex is TypeLoadException ||
                   ex is BadImageFormatException ||
                   ex is AccessViolationException ||
                   ex is VerificationException;
        }

        public static string WriteReport(string source, Exception? ex, bool isTerminating)
        {
            try
            {
                lock (Sync)
                {
                    string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string reportDir = string.IsNullOrEmpty(_baseDir) ? Path.GetTempPath() : _baseDir;
                    string filePath = Path.Combine(reportDir, $"CrashReport_{stamp}.txt");
                    string latestPath = Path.Combine(reportDir, "LatestCrashReport.txt");

                    string content = BuildReportContent(source, ex, isTerminating);
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                    File.WriteAllText(latestPath, content, Encoding.UTF8);
                    ModLogger.Info($"Report written: {filePath}");
                    return filePath;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildReportContent(string source, Exception? ex, bool isTerminating)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("TerrariaModMenu Diagnostic Report");
            sb.AppendLine($"Timestamp     : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Source        : {source}");
            sb.AppendLine($"IsTerminating : {isTerminating}");
            sb.AppendLine($"CLR           : {Environment.Version}");
            sb.AppendLine($"Is64Bit       : {Environment.Is64BitProcess}");
            sb.AppendLine($"OS            : {Environment.OSVersion}");

            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine("[Exception]");
                Exception? cur = ex;
                int depth = 0;
                while (cur != null && depth < 8)
                {
                    sb.AppendLine($"#{depth} {cur.GetType().FullName}: {cur.Message}");
                    sb.AppendLine(cur.StackTrace ?? "(no stack trace)");
                    cur = cur.InnerException;
                    depth++;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"[Hooks] {HarmonyManager.Installed.Count} installed, {HarmonyManager.Failed.Count} failed");
            foreach (string h in HarmonyManager.Installed) sb.AppendLine($"  OK   {h}");
            foreach (string f in HarmonyManager.Failed) sb.AppendLine($"  FAIL {f}");

            sb.AppendLine();
            sb.AppendLine("[Config]");
            try
            {
                var cfg = ModConfig.Instance;
                sb.AppendLine($"  Survival : God={cfg.GodMode}, InfMana={cfg.InfiniteMana}, NoFall={cfg.NoFallDamage}, Flight={cfg.InfiniteFlight}, NoClip={cfg.NoClip}");
                sb.AppendLine($"  RapidFire: Melee={cfg.RapidFireMelee}({cfg.RapidFireMeleeTicks}t), Ranged={cfg.RapidFireRanged}({cfg.RapidFireRangedTicks}t), Magic={cfg.RapidFireMagic}({cfg.RapidFireMagicTicks}t), Throw={cfg.RapidFireThrowables}({cfg.RapidFireThrowablesTicks}t), Tool={cfg.RapidFireTools}({cfg.RapidFireToolsTicks}t), Block={cfg.RapidFireBlocks}({cfg.RapidFireBlocksTicks}t)");
                sb.AppendLine($"  Crafting : NoStations={cfg.CraftIgnoreStations}, Free={cfg.CraftFreeNoConsume}, UnlockAll={cfg.CraftUnlockAllRecipes}");
                sb.AppendLine($"  Potions  : InfDur={cfg.InfiniteBuffDuration}, Purge={cfg.AutoPurgeDebuffs}, NoCD={cfg.NoPotionCooldown}, InfPot={cfg.InfinitePotions}, SuperHeal={cfg.SuperHealAndManaPotions}, OP={cfg.OpPotionsEnabled}({cfg.OpPotionMultiplier:0.0}x)");
                sb.AppendLine($"  World    : Fullbright={cfg.Fullbright}, FreezeTime={cfg.FreezeTime}, DayRate={cfg.CustomDayRate}, SpawnMult={cfg.SpawnRateMultiplier}");
            }
            catch (Exception cfgEx)
            {
                sb.AppendLine($"  error: {cfgEx.Message}");
            }

            sb.AppendLine();
            sb.AppendLine("[GameState]");
            try
            {
                GameSnapshot snap = GameState.CaptureSnapshot();
                sb.AppendLine($"  InWorld={snap.InWorld}, NetMode={snap.NetMode}, Player='{snap.PlayerName}', World='{snap.WorldName}'");
                sb.AppendLine($"  HP={snap.Life}/{snap.LifeMax}, MP={snap.Mana}/{snap.ManaMax}, Def={snap.Defense}, Buffs={snap.ActiveBuffCount}");
                if (snap.HeldItem.Valid)
                {
                    sb.AppendLine($"  Held='{snap.HeldItem.Name}' (id={snap.HeldItem.Type}, cat={snap.HeldItem.Category}, dmg={snap.HeldItem.Damage}, ut={snap.HeldItem.UseTime})");
                }
                sb.AppendLine($"  Loop: GameUpdateCount={Main.GameUpdateCount}, gameMenu={Main.gameMenu}, gamePaused={Main.gamePaused}, CanUpdateGameplay={Main.CanUpdateGameplay}, blockInput={Main.blockInput}, chat={Main.drawingPlayerChat}");
                sb.AppendLine($"  Focus: IsSelected={FocusHelper.IsSelectedApplication}, GameplayActive={FocusHelper.GameplayActive}, StepperPaused={WorldUpdateStepper.Paused}, ignoreErrors={Main.ignoreErrors}");
                if (GameState.TryGetLocalPlayer(out Player? lp) && lp != null)
                {
                    sb.AppendLine($"  Player: pos=({lp.position.X:0.0},{lp.position.Y:0.0}), vel=({lp.velocity.X:0.00},{lp.velocity.Y:0.00}), dead={lp.dead}, ghost={lp.ghost}, CCed={lp.CCed}, frozen={lp.frozen}, stoned={lp.stoned}, webbed={lp.webbed}");
                    sb.AppendLine($"  Controls: U={lp.controlUp}, D={lp.controlDown}, L={lp.controlLeft}, R={lp.controlRight}, Jump={lp.controlJump}, Use={lp.controlUseItem}");
                }
            }
            catch (Exception snapEx)
            {
                sb.AppendLine($"  error: {snapEx.Message}");
            }

            return sb.ToString();
        }
    }
}

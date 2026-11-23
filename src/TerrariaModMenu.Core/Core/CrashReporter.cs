using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.Core
{
    internal static class CrashReporter
    {
        private static readonly object Sync = new object();
        private static string _baseDir = string.Empty;
        private static bool _installed;

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

                _installed = true;
                ModLogger.Info($"CrashReporter initialized ({_baseDir}).");
            }
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
            }
            catch (Exception snapEx)
            {
                sb.AppendLine($"  error: {snapEx.Message}");
            }

            return sb.ToString();
        }
    }
}

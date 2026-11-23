using System;
using System.IO;
using System.Threading;
using Terraria;
using TerrariaModMenu.Core.Patches;

namespace TerrariaModMenu.Core.Core
{
    public enum LaunchMode
    {
        Full = 0,
        Safe = 1
    }

    internal static class ModRuntime
    {
        private static readonly object Sync = new object();
        private static bool _tickAttached;
        private static volatile bool _installPending;
        private static volatile bool _uninstallPending;
        private static volatile bool _hooksReady;
        private static readonly ManualResetEventSlim UninstallDone = new ManualResetEventSlim(false);

        public static LaunchMode CurrentMode { get; private set; } = LaunchMode.Full;
        public static bool HooksReady => _hooksReady;

        public static bool IsFullModeActive =>
            CurrentMode == LaunchMode.Full && Main.netMode == 0 && !Main.dedServ;

        public static void LoadLaunchMode(string baseDirectory)
        {
            CurrentMode = LaunchMode.Full;
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                {
                    return;
                }

                string cfgPath = Path.Combine(baseDirectory, "launch_mode.cfg");
                if (File.Exists(cfgPath))
                {
                    string raw = File.ReadAllText(cfgPath).Trim();
                    if (raw.Equals("SAFE", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentMode = LaunchMode.Safe;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModRuntime.LoadLaunchMode", ex);
            }

            ModLogger.Info($"LaunchMode = {CurrentMode}");
        }

        public static void Start()
        {
            lock (Sync)
            {
                _hooksReady = CurrentMode == LaunchMode.Safe;
                _installPending = CurrentMode == LaunchMode.Full;
                _uninstallPending = false;
                UninstallDone.Reset();

                if (!_tickAttached)
                {
                    Main.OnTickForThirdPartySoftwareOnly += OnGameTick;
                    _tickAttached = true;
                }
            }
        }

        public static void Stop()
        {
            try
            {
                ModConfig.Instance.ResetAll();

                if (CurrentMode == LaunchMode.Full && _tickAttached)
                {
                    _uninstallPending = true;
                    if (!UninstallDone.Wait(500))
                    {
                        HarmonyManager.UninstallAll();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModRuntime.Stop", ex);
            }
            finally
            {
                lock (Sync)
                {
                    if (_tickAttached)
                    {
                        Main.OnTickForThirdPartySoftwareOnly -= OnGameTick;
                        _tickAttached = false;
                    }
                    _hooksReady = false;
                }
            }
        }

        private static void OnGameTick()
        {
            try
            {
                if (_uninstallPending)
                {
                    _uninstallPending = false;
                    HarmonyManager.UninstallAll();
                    UninstallDone.Set();
                    return;
                }

                if (_installPending)
                {
                    _installPending = false;
                    HarmonyManager.InstallAll();
                    _hooksReady = true;
                }

                if (!IsFullModeActive)
                {
                    return;
                }

                GameDispatcher.DrainOnGameThread();

                if (Main.gameMenu)
                {
                    return;
                }

                ModConfig cfg = ModConfig.Instance;
                if (cfg.FreezeTime)
                {
                    Main.dayTime = cfg.FrozenDayTime;
                    Main.time = cfg.FrozenTimeValue;
                    Main.dayRate = 0;
                }
                else if (cfg.CustomDayRate != 1)
                {
                    Main.dayRate = cfg.CustomDayRate;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModRuntime.OnGameTick", ex);
            }
        }
    }
}

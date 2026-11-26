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
        private static volatile bool _detachOnlyPending;
        private static volatile bool _reloadPending;
        private static volatile bool _hooksReady;
        private static volatile bool _hooksDetachedByUser;
        private static Action? _pendingHookCallback;
        private static uint _lastWorldTick;
        private static bool _timeRateDirty;
        private static readonly ManualResetEventSlim UninstallDone = new ManualResetEventSlim(false);

        public static LaunchMode CurrentMode { get; private set; } = LaunchMode.Full;
        public static bool HooksReady => _hooksReady;
        public static bool HooksDetached => _hooksDetachedByUser;

        public static bool IsFullModeActive =>
            CurrentMode == LaunchMode.Full && !_hooksDetachedByUser && Main.netMode == 0 && !Main.dedServ;

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
                _hooksDetachedByUser = false;
                _installPending = CurrentMode == LaunchMode.Full;
                _uninstallPending = false;
                _detachOnlyPending = false;
                _reloadPending = false;
                _pendingHookCallback = null;
                UninstallDone.Reset();

                if (!_tickAttached)
                {
                    Main.OnTickForThirdPartySoftwareOnly += OnGameTick;
                    _tickAttached = true;
                }
            }
        }

        public static void RequestReloadHooks(Action? onCompleted = null)
        {
            if (CurrentMode != LaunchMode.Full)
            {
                onCompleted?.Invoke();
                return;
            }

            lock (Sync)
            {
                ModConfig.Instance.ResetAll();
                _hooksDetachedByUser = false;
                _detachOnlyPending = false;
                _pendingHookCallback = onCompleted;
                _reloadPending = true;
            }
        }

        public static void RequestDetachHooks(Action? onCompleted = null)
        {
            if (CurrentMode != LaunchMode.Full)
            {
                onCompleted?.Invoke();
                return;
            }

            lock (Sync)
            {
                ModConfig.Instance.ResetAll();
                _reloadPending = false;
                _installPending = false;
                _pendingHookCallback = onCompleted;
                _detachOnlyPending = true;
            }
        }

        public static void RequestAttachHooks(Action? onCompleted = null)
        {
            if (CurrentMode != LaunchMode.Full)
            {
                onCompleted?.Invoke();
                return;
            }

            lock (Sync)
            {
                _hooksDetachedByUser = false;
                _detachOnlyPending = false;
                _reloadPending = false;
                _pendingHookCallback = onCompleted;
                _installPending = true;
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
                    GameActions.RestoreVanillaStateImmediate(false);
                    HarmonyManager.UninstallAll();
                    _timeRateDirty = false;
                    UninstallDone.Set();
                    return;
                }

                if (_detachOnlyPending)
                {
                    _detachOnlyPending = false;
                    Action? cb = TakePendingCallback();
                    GameActions.RestoreVanillaStateImmediate(true);
                    HarmonyManager.UninstallAll();
                    _hooksDetachedByUser = true;
                    _hooksReady = false;
                    _timeRateDirty = false;
                    cb?.Invoke();
                    return;
                }

                if (_reloadPending)
                {
                    _reloadPending = false;
                    Action? cb = TakePendingCallback();
                    GameActions.RestoreVanillaStateImmediate(false);
                    HarmonyManager.UninstallAll();
                    HarmonyManager.InstallAll();
                    _hooksDetachedByUser = false;
                    _hooksReady = true;
                    _timeRateDirty = false;
                    if (!Main.gameMenu)
                    {
                        Main.NewText($"[ModMenu] Vanilla state restored & {HarmonyManager.Installed.Count} hooks reloaded", 56, 189, 248);
                    }
                    cb?.Invoke();
                    return;
                }

                if (_installPending)
                {
                    _installPending = false;
                    Action? cb = TakePendingCallback();
                    HarmonyManager.InstallAll();
                    _hooksDetachedByUser = false;
                    _hooksReady = true;
                    cb?.Invoke();
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
                    _timeRateDirty = true;
                }
                else if (cfg.CustomDayRate != 1)
                {
                    Main.dayRate = cfg.CustomDayRate;
                    _timeRateDirty = true;
                }
                else if (_timeRateDirty)
                {
                    Main.dayRate = 1;
                    _timeRateDirty = false;
                }

                uint currentTick = Main.GameUpdateCount;
                if (currentTick != _lastWorldTick)
                {
                    _lastWorldTick = currentTick;
                    ProjectilePatches.TickActiveProjectiles();
                    FishingPatches.TickFishing();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModRuntime.OnGameTick", ex);
            }
        }

        private static Action? TakePendingCallback()
        {
            lock (Sync)
            {
                Action? cb = _pendingHookCallback;
                _pendingHookCallback = null;
                return cb;
            }
        }
    }
}

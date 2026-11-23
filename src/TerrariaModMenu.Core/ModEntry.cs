using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using TerrariaModMenu.Core.Core;
using TerrariaModMenu.Core.UI;

namespace TerrariaModMenu.Core
{
    public static class ModEntry
    {
        private static readonly object LifecycleLock = new object();
        private static string _baseDirectory = string.Empty;
        private static bool _resolverInstalled;
        private static Thread? _uiThread;
        private static MainForm? _activeForm;

        public static int EntryPoint(string pwzArgument)
        {
            try
            {
                _baseDirectory = !string.IsNullOrWhiteSpace(pwzArgument)
                    ? pwzArgument
                    : (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty);

                ModLogger.Initialize(_baseDirectory);
                InstallAssemblyResolver();
                CrashReporter.Install(_baseDirectory);
                ModRuntime.LoadLaunchMode(_baseDirectory);

                ModLogger.Info($"EntryPoint invoked (dir='{_baseDirectory}', mode={ModRuntime.CurrentMode}, x64={Environment.Is64BitProcess}).");
                LaunchOrFocusUi();
                return 0;
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModEntry.EntryPoint", ex);
                return -1;
            }
        }

        private static void InstallAssemblyResolver()
        {
            lock (LifecycleLock)
            {
                if (_resolverInstalled)
                {
                    return;
                }

                AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
                {
                    try
                    {
                        string simpleName = new AssemblyName(args.Name).Name;
                        if (string.IsNullOrEmpty(simpleName) || string.IsNullOrEmpty(_baseDirectory))
                        {
                            return null;
                        }

                        string candidate = Path.Combine(_baseDirectory, simpleName + ".dll");
                        if (File.Exists(candidate))
                        {
                            return Assembly.LoadFrom(candidate);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("ModEntry.AssemblyResolve", ex);
                    }
                    return null;
                };

                _resolverInstalled = true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LaunchOrFocusUi()
        {
            lock (LifecycleLock)
            {
                if (_activeForm != null && !_activeForm.IsDisposed)
                {
                    try
                    {
                        _activeForm.BeginInvoke(new Action(() =>
                        {
                            _activeForm.Show();
                            _activeForm.WindowState = FormWindowState.Normal;
                            _activeForm.BringToFront();
                            _activeForm.Activate();
                        }));
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("ModEntry.BringToFront", ex);
                    }
                    return;
                }

                _uiThread = new Thread(RunUiThread)
                {
                    Name = "TerrariaModMenu.UI",
                    IsBackground = true
                };
                _uiThread.SetApartmentState(ApartmentState.STA);
                _uiThread.Start();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunUiThread()
        {
            try
            {
                Application.ThreadException += (_, args) =>
                {
                    ModLogger.Error("WinForms.ThreadException", args.Exception);
                    CrashReporter.WriteReport("WinForms.ThreadException", args.Exception, false);
                };

                try
                {
                    Application.EnableVisualStyles();
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Application.EnableVisualStyles", ex);
                }

                ModRuntime.Start();

                using (var splash = new SplashForm())
                {
                    Application.Run(splash);
                }

                using var form = new MainForm();
                lock (LifecycleLock)
                {
                    _activeForm = form;
                }

                Application.Run(form);
            }
            catch (Exception ex)
            {
                ModLogger.Error("ModEntry.RunUiThread", ex);
            }
            finally
            {
                lock (LifecycleLock)
                {
                    _activeForm = null;
                    _uiThread = null;
                }

                ModRuntime.Stop();
            }
        }
    }
}

using System;
using System.IO;
using System.Reflection;

namespace TerrariaModMenu.Core.Core
{
    internal static class ModLogger
    {
        private static readonly object Sync = new object();
        private static string _logPath = ResolveDefaultPath();

        public static void Initialize(string? baseDirectory)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(baseDirectory) && Directory.Exists(baseDirectory))
                {
                    _logPath = Path.Combine(baseDirectory, "TerrariaModMenu.Core.log");
                }
            }
            catch
            {
            }
        }

        public static void Info(string message) => Write("INFO", message);

        public static void Error(string context, Exception ex) =>
            Write("ERROR", $"{context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, line);
                }
            }
            catch
            {
            }
        }

        private static string ResolveDefaultPath()
        {
            try
            {
                string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    return Path.Combine(dir, "TerrariaModMenu.Core.log");
                }
            }
            catch
            {
            }

            return Path.Combine(Path.GetTempPath(), "TerrariaModMenu.Core.log");
        }
    }
}

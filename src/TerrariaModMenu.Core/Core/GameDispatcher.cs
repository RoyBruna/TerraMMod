using System;
using System.Collections.Concurrent;

namespace TerrariaModMenu.Core.Core
{
    internal static class GameDispatcher
    {
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        public static void Enqueue(Action action)
        {
            if (action != null)
            {
                Queue.Enqueue(action);
            }
        }

        public static void DrainOnGameThread()
        {
            int processed = 0;
            while (processed < 32 && Queue.TryDequeue(out Action? action))
            {
                processed++;
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ModLogger.Error("GameDispatcher.DrainOnGameThread", ex);
                }
            }
        }
    }
}

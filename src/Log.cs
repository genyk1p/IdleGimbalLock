using UnityEngine;

namespace IdleGimbalLock
{
    internal static class Log
    {
        private const string Prefix = "[IdleGimbalLock] ";

        public static void Info(string message) => Debug.Log(Prefix + message);

        public static void Warning(string message) => Debug.LogWarning(Prefix + message);

        public static void Error(string message) => Debug.LogError(Prefix + message);
    }
}

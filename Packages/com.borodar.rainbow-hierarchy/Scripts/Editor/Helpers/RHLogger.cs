using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    internal static class RHLogger
    {
        private const string TAG = "<b>[RH]</b>";

        public static void Log(string message)
        {
            Debug.Log($"{TAG} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{TAG} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{TAG} {message}");
        }
    }
}
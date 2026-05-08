using System.Collections.Generic;
using UnityEngine;

namespace VOIP
{
    internal static class VoiceLog
    {
        private static readonly Dictionary<string, float> NextLogTimeByKey = new Dictionary<string, float>();

        public static void InfoRateLimited(string key, string message, float intervalSeconds)
        {
            if (!ShouldLog(key, intervalSeconds))
            {
                return;
            }

            VOIPPlugin.Log.LogInfo(message);
        }

        public static void WarningRateLimited(string key, string message, float intervalSeconds)
        {
            if (!ShouldLog(key, intervalSeconds))
            {
                return;
            }

            VOIPPlugin.Log.LogWarning(message);
        }

        private static bool ShouldLog(string key, float intervalSeconds)
        {
            float now = Time.time;
            float next;
            if (NextLogTimeByKey.TryGetValue(key, out next) && now < next)
            {
                return false;
            }

            NextLogTimeByKey[key] = now + Mathf.Max(1f, intervalSeconds);
            return true;
        }
    }
}

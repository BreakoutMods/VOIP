using System.Collections.Generic;
using UnityEngine;

namespace VOIP
{
    internal sealed class VoiceRateLimiter
    {
        private const float BurstSeconds = 1.5f;
        private const float MaxSilenceBeforeResetSeconds = 10f;

        private readonly Dictionary<long, Bucket> _buckets = new Dictionary<long, Bucket>();

        public bool Allow(long senderPeerId)
        {
            float now = Time.time;
            float frameSeconds = Mathf.Max(0.02f, VoiceRuntimeSettings.FrameMilliseconds / 1000f);
            float refillPerSecond = 1f / frameSeconds;
            float capacity = Mathf.Max(3f, refillPerSecond * BurstSeconds);

            Bucket bucket;
            if (!_buckets.TryGetValue(senderPeerId, out bucket))
            {
                bucket = new Bucket(capacity, now);
                _buckets[senderPeerId] = bucket;
            }

            if (now - bucket.LastSeen > MaxSilenceBeforeResetSeconds)
            {
                bucket.Tokens = capacity;
            }
            else
            {
                bucket.Tokens = Mathf.Min(capacity, bucket.Tokens + ((now - bucket.LastSeen) * refillPerSecond));
            }

            bucket.LastSeen = now;

            if (bucket.Tokens < 1f)
            {
                return false;
            }

            bucket.Tokens -= 1f;
            return true;
        }

        private sealed class Bucket
        {
            public float Tokens;
            public float LastSeen;

            public Bucket(float tokens, float lastSeen)
            {
                Tokens = tokens;
                LastSeen = lastSeen;
            }
        }
    }
}

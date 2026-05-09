using UnityEngine;

namespace VOIP
{
    internal sealed class VoiceServer : MonoBehaviour
    {
        private const float SettingsBroadcastInterval = 10f;

        internal static VoiceServer Instance { get; private set; }

        private readonly VoiceRateLimiter _rateLimiter = new VoiceRateLimiter();
        private float _nextSettingsBroadcast;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            if (Time.time >= _nextSettingsBroadcast)
            {
                BroadcastServerSettings();
                _nextSettingsBroadcast = Time.time + SettingsBroadcastInterval;
            }
        }

        public VoicePacket Relay(long senderPeerId, VoicePacket packet)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return null;
            }

            if (!VoiceRuntimeSettings.Enabled)
            {
                return null;
            }

            if (!_rateLimiter.Allow(senderPeerId))
            {
                VoiceLog.WarningRateLimited(
                    "voice-rate-limit-" + senderPeerId,
                    "Dropped voice frames from peer " + senderPeerId + " because they exceeded the configured voice rate.",
                    5f);
                return null;
            }

            Vector3 speakerPosition;
            if (!TryGetServerKnownPosition(senderPeerId, out speakerPosition))
            {
                VoiceLog.WarningRateLimited(
                    "voice-missing-position-" + senderPeerId,
                    "Dropped voice frame from peer " + senderPeerId + " because the server could not resolve its position.",
                    5f);
                return null;
            }

            VoicePacket relayPacket = packet.WithServerSpeaker(senderPeerId, speakerPosition);
            float maxDistance = VoiceRuntimeSettings.ProximityMeters;
            float maxDistanceSquared = maxDistance * maxDistance;

            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer == null || peer.m_uid == senderPeerId)
                {
                    continue;
                }

                if ((peer.GetRefPos() - relayPacket.SpeakerPosition).sqrMagnitude > maxDistanceSquared)
                {
                    continue;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, VoiceNetwork.VoiceFrameRpcName, relayPacket.ToPackage());
            }

            return relayPacket;
        }

        private static bool TryGetServerKnownPosition(long senderPeerId, out Vector3 position)
        {
            if (senderPeerId == ZNet.GetUID() && Player.m_localPlayer != null)
            {
                position = Player.m_localPlayer.transform.position;
                return true;
            }

            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer != null && peer.m_uid == senderPeerId)
                {
                    position = peer.GetRefPos();
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        private static void BroadcastServerSettings()
        {
            if (ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, VoiceNetwork.SettingsRpcName, VoiceRuntimeSettings.CreateServerPackage());
            VoiceLog.InfoRateLimited("voice-settings-broadcast", "Broadcasting server voice settings: " + VoiceRuntimeSettings.CreateServerSummary(), 60f);
        }
    }
}

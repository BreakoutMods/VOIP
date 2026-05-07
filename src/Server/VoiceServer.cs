using UnityEngine;

namespace ValheimVoip
{
    internal sealed class VoiceServer : MonoBehaviour
    {
        private const float SettingsBroadcastInterval = 10f;

        internal static VoiceServer Instance { get; private set; }

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

        public void Relay(long senderPeerId, VoicePacket packet)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            float maxDistance = VoiceRuntimeSettings.ProximityMeters;
            float maxDistanceSquared = maxDistance * maxDistance;

            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer == null || peer.m_uid == senderPeerId)
                {
                    continue;
                }

                if ((peer.GetRefPos() - packet.SpeakerPosition).sqrMagnitude > maxDistanceSquared)
                {
                    continue;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, VoiceNetwork.VoiceFrameRpcName, packet.ToPackage());
            }
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

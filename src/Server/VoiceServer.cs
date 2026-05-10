using UnityEngine;
using BreakoutMods.BreakoutNet;

namespace VOIP
{
    internal sealed class VoiceServer : MonoBehaviour
    {
        internal static VoiceServer Instance { get; private set; }

        private readonly VoiceRateLimiter _rateLimiter = new VoiceRateLimiter();
        private BreakoutModuleContext _context;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(BreakoutModuleContext context)
        {
            _context = context;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public VoicePacket Relay(long senderPeerId, VoicePacket packet)
        {
            if (ZNet.instance == null || !BreakoutSide.IsServer)
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
            int recipients = 0;

            foreach (ZNetPeer peer in BreakoutPeers.ConnectedPeers)
            {
                if (peer == null || peer.m_uid == senderPeerId)
                {
                    continue;
                }

                if ((peer.GetRefPos() - relayPacket.SpeakerPosition).sqrMagnitude > maxDistanceSquared)
                {
                    continue;
                }

                if (BreakoutRpc.Server.SendToClient(peer.m_uid, VoiceNetwork.VoiceFrameRpcName, relayPacket, _context != null ? _context.ModGuid : VOIPPlugin.ModGuid))
                {
                    recipients++;
                }
            }

            if (_context != null)
            {
                VoicePacketRelayedEvent relayedEvent = new VoicePacketRelayedEvent(relayPacket.SpeakerId, recipients, relayPacket.Sequence);
                _context.Events.Publish(relayedEvent);
                _context.Events.Publish("voip.voice.relayed", relayedEvent);
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
    }
}

using UnityEngine;

namespace VOIP
{
    internal sealed class VoiceNetwork : MonoBehaviour
    {
        internal const string VoiceFrameRpcName = "VOIP_VoiceFrame";
        internal const string SettingsRpcName = "VOIP_Settings";

        private VoiceClient _client;
        private VoiceServer _server;
        private VoicePlayback _playback;
        private bool _registered;
        private ZRoutedRpc _registeredRpc;

        public void Initialize(VoiceClient client, VoiceServer server, VoicePlayback playback)
        {
            _client = client;
            _server = server;
            _playback = playback;
        }

        private void Update()
        {
            if (ZRoutedRpc.instance == null)
            {
                _registered = false;
                _registeredRpc = null;
                if (_client != null)
                {
                    _client.OnRpcUnavailable();
                }

                return;
            }

            if (!_registered || _registeredRpc != ZRoutedRpc.instance)
            {
                ZRoutedRpc.instance.Register<ZPackage>(VoiceFrameRpcName, OnVoiceFrame);
                ZRoutedRpc.instance.Register<ZPackage>(SettingsRpcName, OnSettings);
                _registered = true;
                _registeredRpc = ZRoutedRpc.instance;
                VOIPPlugin.Log.LogInfo("Voice RPC registered");
            }
        }

        public void Send(VoicePacket packet)
        {
            if (_client != null)
            {
                _client.Send(packet);
            }
        }

        private void OnVoiceFrame(long senderPeerId, ZPackage package)
        {
            if (!VoiceRuntimeSettings.Enabled)
            {
                return;
            }

            VoicePacket packet;
            try
            {
                packet = VoicePacket.FromPackage(package);
            }
            catch (System.Exception ex)
            {
                VoiceLog.WarningRateLimited(
                    "voice-packet-malformed-" + senderPeerId,
                    "Dropped malformed voice packet from peer " + senderPeerId + ": " + ex.Message,
                    5f);
                return;
            }

            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                VoicePacket relayPacket = _server != null ? _server.Relay(senderPeerId, packet) : null;
                if (relayPacket != null && _playback != null && Player.m_localPlayer != null && relayPacket.SpeakerId != ZNet.GetUID())
                {
                    _playback.Play(relayPacket);
                }

                return;
            }

            if (_playback == null)
            {
                return;
            }

            ZNetPeer serverPeer = ZNet.instance != null ? ZNet.instance.GetServerPeer() : null;
            if (serverPeer == null || serverPeer.m_uid != senderPeerId)
            {
                VoiceLog.WarningRateLimited(
                    "voice-frame-unauthorized-" + senderPeerId,
                    "Ignored voice frame from non-server peer " + senderPeerId + ".",
                    30f);
                return;
            }

            if (packet.SpeakerId == ZNet.GetUID())
            {
                return;
            }

            _playback.Play(packet);
        }

        private void OnSettings(long senderPeerId, ZPackage package)
        {
            if (_client != null)
            {
                _client.ApplyServerSettings(senderPeerId, package);
            }
        }
    }
}

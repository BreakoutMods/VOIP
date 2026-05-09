using UnityEngine;
using BreakoutMods.BreakoutNet;

namespace VOIP
{
    internal sealed class VoiceNetwork : MonoBehaviour
    {
        internal const string VoiceFrameRpcName = "voip.voice.frame";
        internal const string SettingsName = "voip.settings";

        private VoiceClient _client;
        private VoiceServer _server;
        private VoicePlayback _playback;
        private bool _wasInWorld;

        public void Initialize(VoiceClient client, VoiceServer server, VoicePlayback playback)
        {
            _client = client;
            _server = server;
            _playback = playback;

            BreakoutRpc.Server.Register<VoicePacket>(VoiceFrameRpcName, OnServerVoiceFrame);
            BreakoutRpc.Client.Register<VoicePacket>(VoiceFrameRpcName, OnClientVoiceFrame);

            BreakoutSettingsSync.RegisterServerSettings(SettingsName, VoiceRuntimeSettings.CreateServerSettings);
            BreakoutSettingsSync.Client.Register<VoiceServerSettings>(SettingsName, OnClientSettings);

            VOIPPlugin.Log.LogInfo("Voice RPC registered through BreakoutNet.");
        }

        public void Send(VoicePacket packet)
        {
            if (_client != null)
            {
                _client.Send(packet);
            }
        }

        private void Update()
        {
            bool isInWorld = BreakoutSide.IsInWorld;
            if (_wasInWorld && !isInWorld && _client != null)
            {
                _client.OnRpcUnavailable();
            }

            _wasInWorld = isInWorld;
        }

        private void OnServerVoiceFrame(BreakoutRpcContext context, VoicePacket packet)
        {
            if (!VoiceRuntimeSettings.Enabled)
            {
                return;
            }

            VoicePacket relayPacket = _server != null ? _server.Relay(context.SenderPeerId, packet) : null;
            if (relayPacket != null && _playback != null && Player.m_localPlayer != null && relayPacket.SpeakerId != ZNet.GetUID())
            {
                _playback.Play(relayPacket);
            }
        }

        private void OnClientVoiceFrame(BreakoutRpcContext context, VoicePacket packet)
        {
            if (!context.IsFromServer)
            {
                context.Reject("Voice frame must come from the server.");
                return;
            }

            if (_playback == null || packet.SpeakerId == ZNet.GetUID())
            {
                return;
            }

            _playback.Play(packet);
        }

        private void OnClientSettings(VoiceServerSettings settings)
        {
            if (_client != null)
            {
                _client.ApplyServerSettings(settings);
            }
        }
    }
}

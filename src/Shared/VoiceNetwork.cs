using UnityEngine;
using BreakoutMods.BreakoutNet;

namespace VOIP
{
    internal sealed class VoiceNetwork : MonoBehaviour
    {
        internal const string VoiceFrameRpcName = "voip.voice.frame";
        internal const string SettingsName = "voip.settings";

        private BreakoutModuleContext _context;
        private VoiceClient _client;
        private VoiceServer _server;
        private VoicePlayback _playback;

        public void Initialize(BreakoutModuleContext context, VoiceClient client, VoiceServer server, VoicePlayback playback)
        {
            _context = context;
            _client = client;
            _server = server;
            _playback = playback;

            BreakoutRpc.Server.Register<VoicePacket>(VoiceFrameRpcName, OnServerVoiceFrame);
            BreakoutRpc.Client.Register<VoicePacket>(VoiceFrameRpcName, OnClientVoiceFrame);

            BreakoutSettingsSync.RegisterServerSettings(SettingsName, VoiceRuntimeSettings.CreateServerSettings);
            BreakoutSettingsSync.Client.Register<VoiceServerSettings>(SettingsName, OnClientSettings);

            if (_context != null)
            {
                _context.Hooks.OnWorldLeft(OnWorldLeft);
                _context.Hooks.OnRpcRejected(OnRpcRejected);
            }

            VOIPPlugin.Log.LogInfo("Voice RPC registered through BreakoutNet.");
        }

        public void Send(VoicePacket packet)
        {
            if (_client != null)
            {
                _client.Send(packet);
            }
        }

        private void OnWorldLeft(BreakoutWorldLeftEvent evt)
        {
            if (_client != null)
            {
                _client.OnRpcUnavailable();
            }
        }

        private static void OnRpcRejected(BreakoutRpcRejectedEvent evt)
        {
            if (evt.RpcName != VoiceFrameRpcName && evt.RpcName != string.Empty)
            {
                return;
            }

            VoiceLog.WarningRateLimited(
                "voice-breakoutnet-rpc-rejected-" + evt.Category,
                "BreakoutNet rejected a VOIP RPC from peer " + evt.SenderPeerId + ": " + evt.Reason,
                10f);
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

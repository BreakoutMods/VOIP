using BreakoutMods.BreakoutNet;

namespace VOIP
{
    internal sealed class VoiceClient
    {
        public void Send(VoicePacket packet)
        {
            if (!BreakoutSide.IsInWorld || ZNet.instance == null)
            {
                return;
            }

            packet.SenderPeerId = ZNet.GetUID();
            BreakoutRpc.Client.SendToServer(VoiceNetwork.VoiceFrameRpcName, packet, VOIPPlugin.ModGuid);
        }

        public void ApplyServerSettings(VoiceServerSettings settings)
        {
            try
            {
                string summary;
                bool changed = VoiceRuntimeSettings.ApplyServerSettings(settings, out summary);
                if (changed)
                {
                    VOIPPlugin.Log.LogInfo("Applied server voice settings: " + summary);
                }
                else
                {
                    VoiceLog.InfoRateLimited("voice-settings-unchanged", "Received server voice settings: " + summary, 60f);
                }
            }
            catch (System.Exception ex)
            {
                VoiceLog.WarningRateLimited(
                    "voice-settings-malformed",
                    "Dropped malformed voice settings package from server: " + ex.Message,
                    10f);
            }
        }

        public void OnRpcUnavailable()
        {
            VoiceRuntimeSettings.ClearServerSettings();
        }
    }
}

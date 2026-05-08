namespace VOIP
{
    internal sealed class VoiceClient
    {
        public void Send(VoicePacket packet)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null)
            {
                return;
            }

            if (ZNet.instance.IsServer())
            {
                VoiceServer relay = VoiceServer.Instance;
                if (relay != null)
                {
                    relay.Relay(ZNet.GetUID(), packet);
                }

                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(serverPeer.m_uid, VoiceNetwork.VoiceFrameRpcName, packet.ToPackage());
        }

        public void ApplyServerSettings(long senderPeerId, ZPackage package)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
            {
                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null || serverPeer.m_uid != senderPeerId)
            {
                VoiceLog.WarningRateLimited(
                    "voice-settings-unauthorized",
                    "Ignored voice settings package from non-server peer " + senderPeerId + ".",
                    30f);
                return;
            }

            try
            {
                string summary;
                bool changed = VoiceRuntimeSettings.ApplyServerPackage(package, out summary);
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
                    "Dropped malformed voice settings package from server peer " + senderPeerId + ": " + ex.Message,
                    10f);
            }
        }

        public void OnRpcUnavailable()
        {
            VoiceRuntimeSettings.ClearServerSettings();
        }
    }
}

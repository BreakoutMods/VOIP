namespace VOIP
{
    internal static class VoiceValidationHarness
    {
        public static bool InvalidProtocolVersionFails()
        {
            return Fails(CreatePackage(999, 1, VoicePacket.CurrentProtocolVersion, 16000, 960, new byte[] { 1 }));
        }

        public static bool InvalidSampleRateFails()
        {
            return Fails(CreatePackage(VoicePacket.CurrentProtocolVersion, 1, 1, 11025, 220, new byte[] { 1 }));
        }

        public static bool MismatchedSampleCountFails()
        {
            return Fails(CreatePackage(VoicePacket.CurrentProtocolVersion, 1, 1, 16000, 123, new byte[] { 1 }));
        }

        public static bool EmptyPayloadFails()
        {
            return Fails(CreatePackage(VoicePacket.CurrentProtocolVersion, 1, 1, 16000, 960, new byte[0]));
        }

        public static bool OversizedPayloadFails()
        {
            return Fails(CreatePackage(VoicePacket.CurrentProtocolVersion, 1, 1, 16000, 960, new byte[VoicePacket.MaxOpusPayloadBytes + 1]));
        }

        private static bool Fails(ZPackage package)
        {
            try
            {
                VoicePacket.FromPackage(package);
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static ZPackage CreatePackage(int protocolVersion, int sequence, long speakerId, int sampleRate, int samples, byte[] payload)
        {
            ZPackage package = new ZPackage();
            package.Write(protocolVersion);
            package.Write(sequence);
            package.Write(speakerId);
            package.Write(speakerId);
            package.Write(UnityEngine.Vector3.zero);
            package.Write(sampleRate);
            package.Write(samples);
            package.Write(payload);
            return package;
        }
    }
}

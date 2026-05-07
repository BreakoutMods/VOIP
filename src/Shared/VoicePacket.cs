using UnityEngine;

namespace ValheimVoip
{
    internal sealed class VoicePacket
    {
        public long SpeakerId;
        public Vector3 SpeakerPosition;
        public int SampleRate;
        public int Samples;
        public byte[] OpusPayload;

        public ZPackage ToPackage()
        {
            ZPackage package = new ZPackage();
            package.Write(SpeakerId);
            package.Write(SpeakerPosition);
            package.Write(SampleRate);
            package.Write(Samples);
            package.Write(OpusPayload);
            return package;
        }

        public static VoicePacket FromPackage(ZPackage package)
        {
            return new VoicePacket
            {
                SpeakerId = package.ReadLong(),
                SpeakerPosition = package.ReadVector3(),
                SampleRate = package.ReadInt(),
                Samples = package.ReadInt(),
                OpusPayload = package.ReadByteArray()
            };
        }
    }
}

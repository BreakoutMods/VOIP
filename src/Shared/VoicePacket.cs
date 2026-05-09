using UnityEngine;

namespace VOIP
{
    internal sealed class VoicePacket
    {
        public const int CurrentProtocolVersion = 2;
        public const int MaxOpusPayloadBytes = 1275;

        public int ProtocolVersion = CurrentProtocolVersion;
        public int Sequence;
        public long SenderPeerId;
        public long SpeakerId;
        public Vector3 SpeakerPosition;
        public int SampleRate;
        public int Samples;
        public byte[] OpusPayload;

        public ZPackage ToPackage()
        {
            ZPackage package = new ZPackage();
            package.Write(ProtocolVersion);
            package.Write(Sequence);
            package.Write(SenderPeerId);
            package.Write(SpeakerId);
            package.Write(SpeakerPosition);
            package.Write(SampleRate);
            package.Write(Samples);
            package.Write(OpusPayload);
            return package;
        }

        public static VoicePacket FromPackage(ZPackage package)
        {
            VoicePacket packet = new VoicePacket
            {
                ProtocolVersion = package.ReadInt(),
                Sequence = package.ReadInt(),
                SenderPeerId = package.ReadLong(),
                SpeakerId = package.ReadLong(),
                SpeakerPosition = package.ReadVector3(),
                SampleRate = package.ReadInt(),
                Samples = package.ReadInt(),
                OpusPayload = package.ReadByteArray()
            };

            Validate(packet);
            return packet;
        }

        public static void Validate(VoicePacket packet)
        {
            if (packet == null)
            {
                throw new System.ArgumentNullException("packet");
            }

            if (packet.ProtocolVersion != CurrentProtocolVersion)
            {
                throw new System.InvalidOperationException("Unsupported voice protocol version " + packet.ProtocolVersion + ".");
            }

            if (!IsSupportedSampleRate(packet.SampleRate))
            {
                throw new System.InvalidOperationException("Unsupported voice sample rate " + packet.SampleRate + ".");
            }

            if (!IsSupportedSampleCount(packet.SampleRate, packet.Samples))
            {
                throw new System.InvalidOperationException("Unsupported voice sample count " + packet.Samples + " for sample rate " + packet.SampleRate + ".");
            }

            if (packet.OpusPayload == null || packet.OpusPayload.Length == 0)
            {
                throw new System.InvalidOperationException("Voice payload is empty.");
            }

            if (packet.OpusPayload.Length > MaxOpusPayloadBytes)
            {
                throw new System.InvalidOperationException("Voice payload is too large: " + packet.OpusPayload.Length + " bytes.");
            }

            if (!IsFinite(packet.SpeakerPosition))
            {
                throw new System.InvalidOperationException("Voice speaker position is invalid.");
            }
        }

        public VoicePacket WithServerSpeaker(long senderPeerId, Vector3 serverPosition)
        {
            VoicePacket packet = new VoicePacket
            {
                ProtocolVersion = CurrentProtocolVersion,
                Sequence = Sequence,
                SenderPeerId = senderPeerId,
                SpeakerId = senderPeerId,
                SpeakerPosition = serverPosition,
                SampleRate = SampleRate,
                Samples = Samples,
                OpusPayload = OpusPayload
            };

            Validate(packet);
            return packet;
        }

        private static bool IsSupportedSampleRate(int sampleRate)
        {
            return sampleRate == 8000
                || sampleRate == 12000
                || sampleRate == 16000
                || sampleRate == 24000
                || sampleRate == 48000;
        }

        private static bool IsSupportedSampleCount(int sampleRate, int samples)
        {
            return samples == sampleRate * 20 / 1000
                || samples == sampleRate * 40 / 1000
                || samples == sampleRate * 60 / 1000;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

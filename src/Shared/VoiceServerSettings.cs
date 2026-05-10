using BreakoutMods.BreakoutNet;

namespace VOIP
{
    internal sealed class VoiceServerSettings : IBreakoutSerializable
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public bool Enabled;
        public float ProximityMeters;
        public float FullVolumeMeters;
        public int SampleRate;
        public int FrameMilliseconds;
        public int OpusBitrate;
        public int OpusComplexity;

        public void Write(ZPackage package)
        {
            package.Write(Version);
            package.Write(Enabled);
            package.Write(ProximityMeters);
            package.Write(FullVolumeMeters);
            package.Write(SampleRate);
            package.Write(FrameMilliseconds);
            package.Write(OpusBitrate);
            package.Write(OpusComplexity);
        }

        public void Read(ZPackage package)
        {
            Version = package.ReadInt();
            Enabled = package.ReadBool();
            ProximityMeters = package.ReadSingle();
            FullVolumeMeters = package.ReadSingle();
            SampleRate = package.ReadInt();
            FrameMilliseconds = package.ReadInt();
            OpusBitrate = package.ReadInt();
            OpusComplexity = package.ReadInt();
        }
    }
}

using BreakoutMods.BreakoutNet;

namespace VOIP
{
    public sealed class VoiceSettingsAppliedEvent : IBreakoutEvent
    {
        public VoiceSettingsAppliedEvent(bool changed, string summary)
        {
            Changed = changed;
            Summary = summary ?? string.Empty;
        }

        public bool Changed { get; private set; }

        public string Summary { get; private set; }
    }

    public sealed class VoicePacketRelayedEvent : IBreakoutEvent
    {
        public VoicePacketRelayedEvent(long speakerId, int recipients, int sequence)
        {
            SpeakerId = speakerId;
            Recipients = recipients;
            Sequence = sequence;
        }

        public long SpeakerId { get; private set; }

        public int Recipients { get; private set; }

        public int Sequence { get; private set; }
    }
}

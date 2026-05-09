using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VOIP
{
    internal static class VoiceMuteState
    {
        private static readonly HashSet<long> MutedSpeakerIds = new HashSet<long>();
        private static string _loadedConfigValue;

        public static bool Deafened { get; private set; }
        public static long LastSpeakerId { get; private set; }
        public static bool HasLastSpeaker { get; private set; }

        public static void ToggleDeafen()
        {
            Deafened = !Deafened;
            VOIPPlugin.Log.LogInfo("VOIP deafen " + (Deafened ? "enabled" : "disabled") + ".");
        }

        public static void RememberSpeaker(long speakerId)
        {
            LastSpeakerId = speakerId;
            HasLastSpeaker = true;
        }

        public static void ToggleLastSpeakerMute()
        {
            LoadMutedSpeakers();
            if (!HasLastSpeaker)
            {
                VoiceLog.InfoRateLimited("voice-mute-no-speaker", "VOIP has no recent speaker to mute.", 3f);
                return;
            }

            if (MutedSpeakerIds.Contains(LastSpeakerId))
            {
                MutedSpeakerIds.Remove(LastSpeakerId);
                VOIPPlugin.Log.LogInfo("Unmuted VOIP speaker " + LastSpeakerId + ".");
            }
            else
            {
                MutedSpeakerIds.Add(LastSpeakerId);
                VOIPPlugin.Log.LogInfo("Muted VOIP speaker " + LastSpeakerId + ".");
            }

            SaveMutedSpeakers();
        }

        public static bool IsMuted(long speakerId)
        {
            LoadMutedSpeakers();
            return MutedSpeakerIds.Contains(speakerId);
        }

        private static void LoadMutedSpeakers()
        {
            string value = VoiceSettings.MutedSpeakerIds.Value ?? string.Empty;
            if (_loadedConfigValue == value)
            {
                return;
            }

            MutedSpeakerIds.Clear();
            foreach (string part in value.Split(','))
            {
                long speakerId;
                if (long.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out speakerId))
                {
                    MutedSpeakerIds.Add(speakerId);
                }
            }

            _loadedConfigValue = value;
        }

        private static void SaveMutedSpeakers()
        {
            string value = string.Join(",", MutedSpeakerIds.OrderBy(id => id).Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray());
            VoiceSettings.MutedSpeakerIds.Value = value;
            _loadedConfigValue = value;
        }
    }
}

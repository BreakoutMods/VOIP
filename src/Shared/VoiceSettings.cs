using BepInEx.Configuration;
using UnityEngine;

namespace VOIP
{
    internal static class VoiceSettings
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> PushToTalkKey { get; private set; }
        public static ConfigEntry<string> MicrophoneDevice { get; private set; }
        public static ConfigEntry<int> MicrophoneStopDelayMilliseconds { get; private set; }
        public static ConfigEntry<string> ToggleDeafenKey { get; private set; }
        public static ConfigEntry<string> ToggleMuteLastSpeakerKey { get; private set; }
        public static ConfigEntry<string> MutedSpeakerIds { get; private set; }
        public static ConfigEntry<bool> VoiceActivation { get; private set; }
        public static ConfigEntry<float> VoiceActivationThreshold { get; private set; }
        public static ConfigEntry<float> ProximityMeters { get; private set; }
        public static ConfigEntry<float> FullVolumeMeters { get; private set; }
        public static ConfigEntry<float> PlaybackVolume { get; private set; }
        public static ConfigEntry<int> SampleRate { get; private set; }
        public static ConfigEntry<int> FrameMilliseconds { get; private set; }
        public static ConfigEntry<int> JitterBufferMilliseconds { get; private set; }
        public static ConfigEntry<int> MaxJitterBufferMilliseconds { get; private set; }
        public static ConfigEntry<int> OpusBitrate { get; private set; }
        public static ConfigEntry<int> OpusComplexity { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable proximity voice chat.");
            PushToTalkKey = config.Bind("Input", "PushToTalkKey", "V", "Unity KeyCode name used for push-to-talk.");
            MicrophoneDevice = config.Bind("Input", "MicrophoneDevice", "", "Preferred Unity microphone device name. Leave empty to use the default device.");
            MicrophoneStopDelayMilliseconds = config.Bind("Input", "MicrophoneStopDelayMilliseconds", 250, "Delay before stopping the microphone after push-to-talk is released.");
            ToggleDeafenKey = config.Bind("Input", "ToggleDeafenKey", "B", "Unity KeyCode name used to toggle local deafen.");
            ToggleMuteLastSpeakerKey = config.Bind("Input", "ToggleMuteLastSpeakerKey", "M", "Unity KeyCode name used to mute or unmute the last received speaker.");
            MutedSpeakerIds = config.Bind("Moderation", "MutedSpeakerIds", "", "Comma-separated VOIP speaker IDs muted locally.");
            VoiceActivation = config.Bind("Input", "VoiceActivation", false, "Transmit when the microphone level exceeds the configured threshold.");
            VoiceActivationThreshold = config.Bind("Input", "VoiceActivationThreshold", 0.015f, "RMS threshold used when voice activation is enabled.");
            ProximityMeters = config.Bind("Proximity", "ProximityMeters", 35f, "Maximum distance in meters for receiving voice.");
            FullVolumeMeters = config.Bind("Proximity", "FullVolumeMeters", 6f, "Distance in meters where playback is still full volume.");
            PlaybackVolume = config.Bind("Audio", "PlaybackVolume", 1f, "Master playback volume for received voice.");
            SampleRate = config.Bind("Audio", "SampleRate", 16000, "Microphone sample rate. Opus supports 8000, 12000, 16000, 24000, and 48000.");
            FrameMilliseconds = config.Bind("Audio", "FrameMilliseconds", 60, "Captured audio duration per network packet. Opus supports 20, 40, or 60 here.");
            JitterBufferMilliseconds = config.Bind("Audio", "JitterBufferMilliseconds", 120, "Target playback buffer before received voice starts.");
            MaxJitterBufferMilliseconds = config.Bind("Audio", "MaxJitterBufferMilliseconds", 500, "Maximum queued playback audio before old samples are dropped.");
            OpusBitrate = config.Bind("Opus", "Bitrate", 24000, "Target Opus bitrate in bits per second.");
            OpusComplexity = config.Bind("Opus", "Complexity", 5, "Opus encoder complexity from 0 to 10.");
        }

        public static KeyCode PushToTalkKeyCode
        {
            get
            {
                return ParseKeyCode(PushToTalkKey.Value, KeyCode.V);
            }
        }

        public static KeyCode ToggleDeafenKeyCode
        {
            get { return ParseKeyCode(ToggleDeafenKey.Value, KeyCode.B); }
        }

        public static KeyCode ToggleMuteLastSpeakerKeyCode
        {
            get { return ParseKeyCode(ToggleMuteLastSpeakerKey.Value, KeyCode.M); }
        }

        public static int EffectiveSampleRate
        {
            get
            {
                int rate = SampleRate.Value;
                if (rate == 8000 || rate == 12000 || rate == 16000 || rate == 24000 || rate == 48000)
                {
                    return rate;
                }

                return 16000;
            }
        }

        public static int EffectiveFrameMilliseconds
        {
            get
            {
                int ms = FrameMilliseconds.Value;
                if (ms == 20 || ms == 40 || ms == 60)
                {
                    return ms;
                }

                return 60;
            }
        }

        public static int EffectiveOpusComplexity
        {
            get
            {
                return Mathf.Clamp(OpusComplexity.Value, 0, 10);
            }
        }

        public static int EffectiveOpusBitrate
        {
            get
            {
                return Mathf.Clamp(OpusBitrate.Value, 6000, 128000);
            }
        }

        public static int EffectiveJitterBufferMilliseconds
        {
            get { return Mathf.Clamp(JitterBufferMilliseconds.Value, 20, 1000); }
        }

        public static int EffectiveMaxJitterBufferMilliseconds
        {
            get { return Mathf.Clamp(MaxJitterBufferMilliseconds.Value, EffectiveJitterBufferMilliseconds, 2000); }
        }

        public static int EffectiveMicrophoneStopDelayMilliseconds
        {
            get { return Mathf.Clamp(MicrophoneStopDelayMilliseconds.Value, 0, 5000); }
        }

        private static KeyCode ParseKeyCode(string value, KeyCode fallback)
        {
            KeyCode parsed;
            return System.Enum.TryParse(value, true, out parsed) ? parsed : fallback;
        }
    }
}

using UnityEngine;

namespace VOIP
{
    internal static class VoiceRuntimeSettings
    {
        private static bool _hasServerSettings;
        private static bool _enabled;
        private static float _proximityMeters;
        private static float _fullVolumeMeters;
        private static int _sampleRate;
        private static int _frameMilliseconds;
        private static int _opusBitrate;
        private static int _opusComplexity;
        private static string _lastAppliedSummary = string.Empty;

        public static bool Enabled
        {
            get { return _hasServerSettings ? _enabled : VoiceSettings.Enabled.Value; }
        }

        public static float ProximityMeters
        {
            get { return _hasServerSettings ? _proximityMeters : VoiceSettings.ProximityMeters.Value; }
        }

        public static float FullVolumeMeters
        {
            get { return _hasServerSettings ? _fullVolumeMeters : VoiceSettings.FullVolumeMeters.Value; }
        }

        public static int SampleRate
        {
            get { return _hasServerSettings ? _sampleRate : VoiceSettings.EffectiveSampleRate; }
        }

        public static int FrameMilliseconds
        {
            get { return _hasServerSettings ? _frameMilliseconds : VoiceSettings.EffectiveFrameMilliseconds; }
        }

        public static int OpusBitrate
        {
            get { return _hasServerSettings ? _opusBitrate : VoiceSettings.EffectiveOpusBitrate; }
        }

        public static int OpusComplexity
        {
            get { return _hasServerSettings ? _opusComplexity : VoiceSettings.EffectiveOpusComplexity; }
        }

        public static void ClearServerSettings()
        {
            if (_hasServerSettings)
            {
                VoiceLog.InfoRateLimited("voice-settings-cleared", "Cleared server voice settings; using local config until the server syncs again.", 10f);
            }

            _hasServerSettings = false;
            _lastAppliedSummary = string.Empty;
        }

        public static VoiceServerSettings CreateServerSettings()
        {
            return new VoiceServerSettings
            {
                Enabled = VoiceSettings.Enabled.Value,
                ProximityMeters = VoiceSettings.ProximityMeters.Value,
                FullVolumeMeters = VoiceSettings.FullVolumeMeters.Value,
                SampleRate = VoiceSettings.EffectiveSampleRate,
                FrameMilliseconds = VoiceSettings.EffectiveFrameMilliseconds,
                OpusBitrate = VoiceSettings.EffectiveOpusBitrate,
                OpusComplexity = VoiceSettings.EffectiveOpusComplexity
            };
        }

        public static ZPackage CreateServerPackage()
        {
            ZPackage package = new ZPackage();
            CreateServerSettings().Write(package);
            package.SetPos(0);
            return package;
        }

        public static bool ApplyServerPackage(ZPackage package, out string summary)
        {
            VoiceServerSettings settings = new VoiceServerSettings();
            settings.Read(package);
            return ApplyServerSettings(settings, out summary);
        }

        public static bool ApplyServerSettings(VoiceServerSettings settings, out string summary)
        {
            summary = string.Empty;
            if (settings.Version != VoiceServerSettings.CurrentVersion)
            {
                VoiceLog.WarningRateLimited("voice-settings-version", "Ignored unsupported voice settings package version " + settings.Version + ".", 30f);
                return false;
            }

            _enabled = settings.Enabled;
            _proximityMeters = Mathf.Max(1f, settings.ProximityMeters);
            _fullVolumeMeters = Mathf.Clamp(settings.FullVolumeMeters, 0.1f, _proximityMeters);
            _sampleRate = SanitizeSampleRate(settings.SampleRate);
            _frameMilliseconds = SanitizeFrameMilliseconds(settings.FrameMilliseconds);
            _opusBitrate = Mathf.Clamp(settings.OpusBitrate, 6000, 128000);
            _opusComplexity = Mathf.Clamp(settings.OpusComplexity, 0, 10);
            _hasServerSettings = true;
            summary = CreateSummary(_enabled, _proximityMeters, _fullVolumeMeters, _sampleRate, _frameMilliseconds, _opusBitrate, _opusComplexity);
            bool changed = summary != _lastAppliedSummary;
            _lastAppliedSummary = summary;
            return changed;
        }

        public static string CreateServerSummary()
        {
            return CreateSummary(
                VoiceSettings.Enabled.Value,
                VoiceSettings.ProximityMeters.Value,
                VoiceSettings.FullVolumeMeters.Value,
                VoiceSettings.EffectiveSampleRate,
                VoiceSettings.EffectiveFrameMilliseconds,
                VoiceSettings.EffectiveOpusBitrate,
                VoiceSettings.EffectiveOpusComplexity);
        }

        private static string CreateSummary(bool enabled, float proximityMeters, float fullVolumeMeters, int sampleRate, int frameMilliseconds, int opusBitrate, int opusComplexity)
        {
            return "enabled=" + enabled +
                   ", proximity=" + proximityMeters.ToString("0.#") +
                   "m, fullVolume=" + fullVolumeMeters.ToString("0.#") +
                   "m, sampleRate=" + sampleRate +
                   ", frameMs=" + frameMilliseconds +
                   ", bitrate=" + opusBitrate +
                   ", complexity=" + opusComplexity;
        }

        private static int SanitizeSampleRate(int rate)
        {
            if (rate == 8000 || rate == 12000 || rate == 16000 || rate == 24000 || rate == 48000)
            {
                return rate;
            }

            return 16000;
        }

        private static int SanitizeFrameMilliseconds(int ms)
        {
            if (ms == 20 || ms == 40 || ms == 60)
            {
                return ms;
            }

            return 60;
        }
    }
}

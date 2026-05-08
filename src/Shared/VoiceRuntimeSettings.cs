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

        public static ZPackage CreateServerPackage()
        {
            ZPackage package = new ZPackage();
            package.Write(1);
            package.Write(VoiceSettings.Enabled.Value);
            package.Write(VoiceSettings.ProximityMeters.Value);
            package.Write(VoiceSettings.FullVolumeMeters.Value);
            package.Write(VoiceSettings.EffectiveSampleRate);
            package.Write(VoiceSettings.EffectiveFrameMilliseconds);
            package.Write(VoiceSettings.EffectiveOpusBitrate);
            package.Write(VoiceSettings.EffectiveOpusComplexity);
            return package;
        }

        public static bool ApplyServerPackage(ZPackage package, out string summary)
        {
            summary = string.Empty;
            int version = package.ReadInt();
            if (version != 1)
            {
                VoiceLog.WarningRateLimited("voice-settings-version", "Ignored unsupported voice settings package version " + version + ".", 30f);
                return false;
            }

            _enabled = package.ReadBool();
            _proximityMeters = Mathf.Max(1f, package.ReadSingle());
            _fullVolumeMeters = Mathf.Clamp(package.ReadSingle(), 0.1f, _proximityMeters);
            _sampleRate = SanitizeSampleRate(package.ReadInt());
            _frameMilliseconds = SanitizeFrameMilliseconds(package.ReadInt());
            _opusBitrate = Mathf.Clamp(package.ReadInt(), 6000, 128000);
            _opusComplexity = Mathf.Clamp(package.ReadInt(), 0, 10);
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

using UnityEngine;

namespace VOIP
{
    internal sealed class VoiceCapture : MonoBehaviour
    {
        private const int MicrophoneBufferSeconds = 1;
        private static readonly System.Reflection.MethodInfo AudioClipGetData = typeof(AudioClip).GetMethod("GetData", new[] { typeof(float[]), typeof(int) });

        private VoiceNetwork _network;
        private AudioClip _microphoneClip;
        private string _device;
        private int _lastPosition;
        private int _sequence;
        private int _activeSampleRate;
        private int _activeFrameMilliseconds;
        private float _stopMicrophoneAt;
        private float[] _frameBuffer;
        private float[] _scratch;
        private readonly OpusVoiceCodec _codec = new OpusVoiceCodec();

        public void Initialize(VoiceNetwork network)
        {
            _network = network;
        }

        private void Update()
        {
            if (!VoiceRuntimeSettings.Enabled || ZNet.instance == null || !IsInWorld())
            {
                StopMicrophone();
                return;
            }

            if (!ShouldTransmit())
            {
                if (!VoiceSettings.VoiceActivation.Value && _microphoneClip != null && Time.time >= _stopMicrophoneAt)
                {
                    StopMicrophone();
                }

                return;
            }

            _stopMicrophoneAt = Time.time + (VoiceSettings.EffectiveMicrophoneStopDelayMilliseconds / 1000f);
            EnsureMicrophone();
            CaptureAvailableFrames();
        }

        private static bool IsInWorld()
        {
            return Player.m_localPlayer != null && ZRoutedRpc.instance != null;
        }

        private static bool ShouldTransmit()
        {
            return VoiceSettings.VoiceActivation.Value || Input.GetKey(VoiceSettings.PushToTalkKeyCode);
        }

        private void EnsureMicrophone()
        {
            int sampleRate = VoiceRuntimeSettings.SampleRate;
            int frameMilliseconds = VoiceRuntimeSettings.FrameMilliseconds;
            string preferredDevice = ResolveMicrophoneDevice();

            if (_microphoneClip != null &&
                Microphone.IsRecording(_device) &&
                _activeSampleRate == sampleRate &&
                _activeFrameMilliseconds == frameMilliseconds &&
                _device == preferredDevice)
            {
                return;
            }

            StopMicrophone();

            if (Microphone.devices.Length == 0)
            {
                VoiceLog.WarningRateLimited("voice-no-microphone", "VOIP could not find a microphone device.", 10f);
                return;
            }

            _device = preferredDevice;
            _microphoneClip = Microphone.Start(_device, true, MicrophoneBufferSeconds, sampleRate);
            _lastPosition = 0;
            _activeSampleRate = sampleRate;
            _activeFrameMilliseconds = frameMilliseconds;

            int frameSamples = sampleRate * frameMilliseconds / 1000;
            _frameBuffer = new float[frameSamples];
            _scratch = new float[_microphoneClip.samples];
        }

        private static string ResolveMicrophoneDevice()
        {
            string configured = VoiceSettings.MicrophoneDevice.Value;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                foreach (string device in Microphone.devices)
                {
                    if (string.Equals(device, configured, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return device;
                    }
                }

                VoiceLog.WarningRateLimited("voice-microphone-missing-" + configured, "Configured VOIP microphone device was not found: " + configured, 10f);
            }

            return Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        }

        private void StopMicrophone()
        {
            if (_microphoneClip == null)
            {
                return;
            }

            if (Microphone.IsRecording(_device))
            {
                Microphone.End(_device);
            }

            _microphoneClip = null;
            _lastPosition = 0;
            _frameBuffer = null;
            _scratch = null;
        }

        private void CaptureAvailableFrames()
        {
            if (_microphoneClip == null)
            {
                return;
            }

            int position = Microphone.GetPosition(_device);
            if (position < 0 || position == _lastPosition)
            {
                return;
            }

            int available = position > _lastPosition
                ? position - _lastPosition
                : (_microphoneClip.samples - _lastPosition) + position;

            while (available >= _frameBuffer.Length)
            {
                ReadFrame(_lastPosition, _frameBuffer);
                _lastPosition = (_lastPosition + _frameBuffer.Length) % _microphoneClip.samples;
                available -= _frameBuffer.Length;
                TrySendFrame(_frameBuffer);
            }
        }

        private void ReadFrame(int startPosition, float[] destination)
        {
            if (startPosition + destination.Length <= _microphoneClip.samples)
            {
                GetAudioClipData(_microphoneClip, destination, startPosition);
                return;
            }

            GetAudioClipData(_microphoneClip, _scratch, 0);
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = _scratch[(startPosition + i) % _microphoneClip.samples];
            }
        }

        private static void GetAudioClipData(AudioClip clip, float[] destination, int offsetSamples)
        {
            if (AudioClipGetData == null)
            {
                throw new System.MissingMethodException("UnityEngine.AudioClip.GetData(float[], int)");
            }

            AudioClipGetData.Invoke(clip, new object[] { destination, offsetSamples });
        }

        private void TrySendFrame(float[] frame)
        {
            if (VoiceSettings.VoiceActivation.Value &&
                AudioMath.Rms(frame, frame.Length) < VoiceSettings.VoiceActivationThreshold.Value)
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return;
            }

            VoicePacket packet = new VoicePacket
            {
                SpeakerId = localPlayer.GetPlayerID(),
                SenderPeerId = ZNet.GetUID(),
                Sequence = ++_sequence,
                SpeakerPosition = localPlayer.transform.position,
                SampleRate = VoiceRuntimeSettings.SampleRate,
                Samples = frame.Length,
                OpusPayload = _codec.Encode(frame, frame.Length, VoiceRuntimeSettings.SampleRate)
            };

            _network.Send(packet);
            VoiceHud.MarkTransmitting();
        }
    }
}

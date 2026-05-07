using UnityEngine;

namespace ValheimVoip
{
    internal sealed class VoiceCapture : MonoBehaviour
    {
        private const int MicrophoneBufferSeconds = 1;

        private VoiceNetwork _network;
        private AudioClip _microphoneClip;
        private string _device;
        private int _lastPosition;
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
                return;
            }

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
            if (_microphoneClip != null && Microphone.IsRecording(_device))
            {
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                return;
            }

            _device = Microphone.devices[0];
            int sampleRate = VoiceRuntimeSettings.SampleRate;
            _microphoneClip = Microphone.Start(_device, true, MicrophoneBufferSeconds, sampleRate);
            _lastPosition = 0;

            int frameSamples = sampleRate * VoiceRuntimeSettings.FrameMilliseconds / 1000;
            _frameBuffer = new float[frameSamples];
            _scratch = new float[_microphoneClip.samples];
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
                _microphoneClip.GetData(destination, startPosition);
                return;
            }

            _microphoneClip.GetData(_scratch, 0);
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = _scratch[(startPosition + i) % _microphoneClip.samples];
            }
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
                SpeakerPosition = localPlayer.transform.position,
                SampleRate = VoiceRuntimeSettings.SampleRate,
                Samples = frame.Length,
                OpusPayload = _codec.Encode(frame, frame.Length, VoiceRuntimeSettings.SampleRate)
            };

            _network.Send(packet);
        }
    }
}

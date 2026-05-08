using System;
using Concentus.Enums;
using Concentus.Structs;

namespace VOIP
{
#pragma warning disable 618
    internal sealed class OpusVoiceCodec
    {
        private const int Channels = 1;
        private const int MaxOpusPacketBytes = 1275;

        private OpusEncoder _encoder;
        private OpusDecoder _decoder;
        private int _encoderSampleRate;
        private int _decoderSampleRate;
        private int _bitrate;
        private int _complexity;
        private short[] _encodeBuffer;
        private short[] _decodeBuffer;
        private byte[] _packetBuffer;

        public byte[] Encode(float[] samples, int count, int sampleRate)
        {
            EnsureEncoder(sampleRate, count);

            for (int i = 0; i < count; i++)
            {
                float clamped = Math.Max(-1f, Math.Min(1f, samples[i]));
                _encodeBuffer[i] = (short)Math.Round(clamped * short.MaxValue);
            }

            int encodedBytes = _encoder.Encode(_encodeBuffer, 0, count, _packetBuffer, 0, _packetBuffer.Length);
            byte[] encoded = new byte[encodedBytes];
            Buffer.BlockCopy(_packetBuffer, 0, encoded, 0, encodedBytes);
            return encoded;
        }

        public float[] Decode(byte[] payload, int samples, int sampleRate)
        {
            EnsureDecoder(sampleRate, samples);

            int decodedSamples = _decoder.Decode(payload, 0, payload.Length, _decodeBuffer, 0, samples, false);
            float[] decoded = new float[decodedSamples];

            for (int i = 0; i < decodedSamples; i++)
            {
                decoded[i] = _decodeBuffer[i] / 32768f;
            }

            return decoded;
        }

        private void EnsureEncoder(int sampleRate, int frameSamples)
        {
            int bitrate = VoiceRuntimeSettings.OpusBitrate;
            int complexity = VoiceRuntimeSettings.OpusComplexity;

            if (_encoder == null ||
                _encoderSampleRate != sampleRate ||
                _bitrate != bitrate ||
                _complexity != complexity)
            {
                _encoder = new OpusEncoder(sampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
                _encoder.Bitrate = bitrate;
                _encoder.Complexity = complexity;
                _encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
                _encoder.UseVBR = true;
                _encoder.UseConstrainedVBR = true;
                _encoderSampleRate = sampleRate;
                _bitrate = bitrate;
                _complexity = complexity;
            }

            if (_encodeBuffer == null || _encodeBuffer.Length < frameSamples)
            {
                _encodeBuffer = new short[frameSamples];
            }

            if (_packetBuffer == null)
            {
                _packetBuffer = new byte[MaxOpusPacketBytes];
            }
        }

        private void EnsureDecoder(int sampleRate, int frameSamples)
        {
            if (_decoder == null || _decoderSampleRate != sampleRate)
            {
                _decoder = new OpusDecoder(sampleRate, Channels);
                _decoderSampleRate = sampleRate;
            }

            if (_decodeBuffer == null || _decodeBuffer.Length < frameSamples)
            {
                _decodeBuffer = new short[frameSamples];
            }
        }
    }
#pragma warning restore 618
}

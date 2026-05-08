using System.Collections.Generic;
using UnityEngine;

namespace VOIP
{
    internal sealed class VoicePlayback : MonoBehaviour
    {
        private const float SpeakerIdleDestroySeconds = 10f;

        private readonly Dictionary<long, SpeakerPlayback> _speakers = new Dictionary<long, SpeakerPlayback>();

        public void Play(VoicePacket packet)
        {
            if (packet.OpusPayload == null || packet.OpusPayload.Length == 0 || packet.Samples <= 0)
            {
                return;
            }

            SpeakerPlayback speaker = GetSpeaker(packet.SpeakerId, packet.SampleRate);
            speaker.UpdateSource(packet.SpeakerPosition);

            try
            {
                speaker.Enqueue(speaker.Codec.Decode(packet.OpusPayload, packet.Samples, packet.SampleRate));
            }
            catch (System.Exception ex)
            {
                VoiceLog.WarningRateLimited(
                    "voice-decode-failed-" + packet.SpeakerId,
                    "Dropped undecodable voice frame from " + packet.SpeakerId + ": " + ex.Message,
                    5f);
            }
        }

        private void Update()
        {
            List<long> stale = null;
            foreach (KeyValuePair<long, SpeakerPlayback> item in _speakers)
            {
                item.Value.UpdatePlayback();
                if (Time.time - item.Value.LastPacketTime > SpeakerIdleDestroySeconds)
                {
                    if (stale == null)
                    {
                        stale = new List<long>();
                    }

                    stale.Add(item.Key);
                }
            }

            if (stale == null)
            {
                return;
            }

            foreach (long speakerId in stale)
            {
                SpeakerPlayback speaker;
                if (_speakers.TryGetValue(speakerId, out speaker))
                {
                    speaker.Destroy();
                    _speakers.Remove(speakerId);
                }
            }
        }

        private SpeakerPlayback GetSpeaker(long speakerId, int sampleRate)
        {
            SpeakerPlayback speaker;
            if (_speakers.TryGetValue(speakerId, out speaker) && speaker != null)
            {
                speaker.EnsureSampleRate(sampleRate);
                return speaker;
            }

            GameObject sourceObject = new GameObject("VOIP Speaker " + speakerId);
            DontDestroyOnLoad(sourceObject);
            speaker = new SpeakerPlayback(speakerId, sourceObject, sampleRate);
            _speakers[speakerId] = speaker;
            return speaker;
        }

        private void OnDestroy()
        {
            foreach (SpeakerPlayback speaker in _speakers.Values)
            {
                speaker.Destroy();
            }

            _speakers.Clear();
        }

        private sealed class SpeakerPlayback
        {
            private readonly object _lock = new object();
            private readonly Queue<float> _samples = new Queue<float>();
            private readonly GameObject _sourceObject;
            private AudioClip _clip;
            private int _bufferedSamples;
            private int _sampleRate;
            private int _underflowEvents;
            private int _droppedSamples;
            private float _nextBufferLog;

            public readonly long SpeakerId;
            public readonly AudioSource Source;
            public readonly OpusVoiceCodec Codec = new OpusVoiceCodec();
            public float LastPacketTime { get; private set; }

            public SpeakerPlayback(long speakerId, GameObject sourceObject, int sampleRate)
            {
                SpeakerId = speakerId;
                _sourceObject = sourceObject;
                Source = sourceObject.AddComponent<AudioSource>();
                Source.playOnAwake = false;
                Source.spatialBlend = 1f;
                Source.rolloffMode = AudioRolloffMode.Linear;
                Source.dopplerLevel = 0f;
                EnsureSampleRate(sampleRate);
            }

            public void EnsureSampleRate(int sampleRate)
            {
                sampleRate = Mathf.Max(8000, sampleRate);
                if (_clip != null && _sampleRate == sampleRate)
                {
                    return;
                }

                _sampleRate = sampleRate;
                lock (_lock)
                {
                    _samples.Clear();
                    _bufferedSamples = 0;
                }

                if (Source.isPlaying)
                {
                    Source.Stop();
                }

                _clip = AudioClip.Create(
                    "voip-stream-" + SpeakerId,
                    _sampleRate,
                    1,
                    _sampleRate,
                    true,
                    OnAudioRead,
                    OnAudioSetPosition);
                Source.clip = _clip;
                Source.loop = true;
            }

            public void UpdateSource(Vector3 position)
            {
                LastPacketTime = Time.time;
                Source.transform.position = position;
                Source.maxDistance = VoiceRuntimeSettings.ProximityMeters;
                Source.minDistance = VoiceRuntimeSettings.FullVolumeMeters;
                Source.volume = VoiceSettings.PlaybackVolume.Value;
            }

            public void Enqueue(float[] decodedSamples)
            {
                if (decodedSamples == null || decodedSamples.Length == 0)
                {
                    return;
                }

                int maxSamples = Mathf.Max(1, _sampleRate * VoiceSettings.EffectiveMaxJitterBufferMilliseconds / 1000);
                int dropped = 0;
                lock (_lock)
                {
                    foreach (float sample in decodedSamples)
                    {
                        _samples.Enqueue(sample);
                    }

                    _bufferedSamples += decodedSamples.Length;
                    while (_bufferedSamples > maxSamples && _samples.Count > 0)
                    {
                        _samples.Dequeue();
                        _bufferedSamples--;
                        dropped++;
                    }

                    _droppedSamples += dropped;
                }
            }

            public void UpdatePlayback()
            {
                int targetSamples = Mathf.Max(1, _sampleRate * VoiceSettings.EffectiveJitterBufferMilliseconds / 1000);
                if (!Source.isPlaying && BufferedSamples >= targetSamples)
                {
                    Source.Play();
                    VoiceLog.InfoRateLimited(
                        "voice-jitter-start-" + SpeakerId,
                        "Started jitter-buffered playback for " + SpeakerId + " with " + BufferedSamples + " queued samples.",
                        10f);
                }

                if (Time.time >= _nextBufferLog)
                {
                    int underflows;
                    int dropped;
                    lock (_lock)
                    {
                        underflows = _underflowEvents;
                        dropped = _droppedSamples;
                        _underflowEvents = 0;
                        _droppedSamples = 0;
                    }

                    _nextBufferLog = Time.time + 10f;

                    if (underflows > 0)
                    {
                        VOIPPlugin.Log.LogWarning("Voice jitter buffer underflow for " + SpeakerId + " (" + underflows + " silent samples in the last window).");
                    }

                    if (dropped > 0)
                    {
                        VOIPPlugin.Log.LogWarning("Voice jitter buffer dropped " + dropped + " old samples for " + SpeakerId + " to stay within the max buffer.");
                    }
                }
            }

            public void Destroy()
            {
                if (_sourceObject != null)
                {
                    Object.Destroy(_sourceObject);
                }
            }

            private int BufferedSamples
            {
                get
                {
                    lock (_lock)
                    {
                        return _bufferedSamples;
                    }
                }
            }

            private void OnAudioRead(float[] data)
            {
                lock (_lock)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (_samples.Count > 0)
                        {
                            data[i] = _samples.Dequeue();
                            _bufferedSamples--;
                        }
                        else
                        {
                            data[i] = 0f;
                            _underflowEvents++;
                        }
                    }
                }
            }

            private void OnAudioSetPosition(int position)
            {
            }
        }
    }
}

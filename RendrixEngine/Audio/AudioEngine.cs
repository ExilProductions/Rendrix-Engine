using System;
using System.Collections.Generic;
using System.Numerics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace RendrixEngine.Audio
{
    internal sealed class AudioEngine : IDisposable
    {
        public static AudioEngine Instance { get; } = new AudioEngine();

        private readonly object _lock = new();
        private readonly List<AudioSource> _sources = new();
        private WaveOutEvent? _outputDevice;
        private MixingSampleProvider? _mixer;
        private bool _initialized;
        private WaveFormat _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private AudioEngine() { }

        public void Initialize()
        {
            if (_initialized) return;

            _outputDevice = new WaveOutEvent
            {
                DesiredLatency = 60
            };
            _mixer = new MixingSampleProvider(_waveFormat)
            {
                ReadFully = true
            };
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
            _initialized = true;
        }

        internal void Register(AudioSource src)
        {
            if (!_initialized) Initialize();
            if (src.Provider == null) return;
            lock (_lock)
            {
                if (!_sources.Contains(src))
                {
                    _sources.Add(src);
                    try
                    {
                        _mixer?.AddMixerInput(src.Provider);
                    }
                    catch (InvalidOperationException)
                    {
                        // WaveFormat mismatch or other issue – ignore for now.
                    }
                }
            }
        }

        internal void Unregister(AudioSource src)
        {
            lock (_lock)
            {
                if (_sources.Remove(src))
                {
                    if (src.Provider != null)
                    {
                        // MixingSampleProvider does not support removal directly; we mark muted.
                        src.MarkStopped();
                    }
                }
            }
        }

        public void Update()
        {
            if (!_initialized) return;
            AudioListener listener = AudioListener.Main;
            if (listener == null) return;

            Vector3 lPos = listener.Transform.Position;
            Vector3 lRight = listener.Transform.Right;

            lock (_lock)
            {
                for (int i = _sources.Count - 1; i >= 0; i--)
                {
                    var src = _sources[i];
                    if (!src.Enabled)
                        continue;
                    if (!src.IsPlaying)
                        continue;
                    if (src.Provider == null)
                        continue;

                    Vector3 sPos = src.Transform.Position;
                    Vector3 toSource = sPos - lPos;
                    float dist = toSource.Length();
                    if (dist < 1e-4f)
                        dist = 0f;

                    float volMul;
                    if (dist <= src.MinDistance)
                        volMul = 1f;
                    else if (dist >= src.MaxDistance)
                        volMul = 0f;
                    else
                        volMul = 1f - (dist - src.MinDistance) / (src.MaxDistance - src.MinDistance);

                    Vector3 dirNorm = toSource;
                    if (dirNorm != Vector3.Zero)
                        dirNorm = Vector3.Normalize(dirNorm);
                    float pan = 0f;
                    if (dirNorm != Vector3.Zero)
                        pan = Math.Clamp(Vector3.Dot(lRight, dirNorm), -1f, 1f);

                    src.Provider.DynamicVolume = volMul;
                    src.Provider.Pan = pan;

                    if (src.Provider.Finished && !src.Loop)
                    {
                        // auto remove finished non-looping sources
                        src.IsPlaying = false;
                        _sources.RemoveAt(i);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var s in _sources)
                {
                    s.Provider?.Dispose();
                }
                _sources.Clear();
            }
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _outputDevice = null;
            _mixer = null;
            _initialized = false;
        }
    }

    internal sealed class SpatialAudioSourceProvider : ISampleProvider, IDisposable
    {
        private readonly AudioFileReader _reader;
        private bool _disposed;
        public bool Loop { get; set; }
        public bool Finished { get; set; }

        public float BaseVolume { get; set; } = 1f;        // user set volume
        public float DynamicVolume { get; set; } = 1f;     // spatial attenuation
        public float Pan { get; set; } = 0f;               // -1 left, 0 center, 1 right

        public WaveFormat WaveFormat => _reader.WaveFormat; // should be stereo float

        public SpatialAudioSourceProvider(string path, bool loop)
        {
            _reader = new AudioFileReader(path); // converts to IEEE float 32 stereo
            Loop = loop;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (Finished)
            {
                // fill silence
                Array.Clear(buffer, offset, count);
                return count;
            }

            int samplesRead = _reader.Read(buffer, offset, count);

            if (samplesRead == 0)
            {
                if (Loop)
                {
                    _reader.Position = 0;
                    samplesRead = _reader.Read(buffer, offset, count);
                }
                else
                {
                    Finished = true;
                    Array.Clear(buffer, offset, count);
                    return count; // remain silent after finish
                }
            }

            // Ensure stereo
            if (WaveFormat.Channels != 2)
            {
                // naive: duplicate channel(s) to fill stereo
                for (int i = 0; i < samplesRead; i++)
                {
                    buffer[offset + i] = buffer[offset + (i % WaveFormat.Channels)];
                }
            }

            float finalVolume = Math.Clamp(BaseVolume * DynamicVolume, 0f, 1f);
            float panClamped = Math.Clamp(Pan, -1f, 1f);
            // Equal-power panning
            float angle = (panClamped + 1f) * 0.25f * MathF.PI; // map [-1,1] -> [0, PI/2]
            float leftGain = MathF.Cos(angle) * finalVolume;
            float rightGain = MathF.Sin(angle) * finalVolume;

            for (int i = 0; i + 1 < samplesRead; i += 2)
            {
                int idx = offset + i;
                float left = buffer[idx];
                float right = buffer[idx + 1];
                float mono = (left + right) * 0.5f;
                buffer[idx] = mono * leftGain;
                buffer[idx + 1] = mono * rightGain;
            }
            return samplesRead;
        }

        public void Reset()
        {
            _reader.Position = 0;
            Finished = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _reader.Dispose();
            _disposed = true;
        }
    }
}

using System;
using RendrixEngine.Audio;

namespace RendrixEngine
{
    public sealed class AudioSource : Component, IDisposable
    {
        internal SpatialAudioSourceProvider? Provider { get; private set; }
        public bool Loop { get; set; }
        public bool PlayOnAwake { get; set; } = true;
        public float Volume { get; set; } = 1f;
        public float MinDistance { get; set; } = 1f; // distance with full volume
        public float MaxDistance { get; set; } = 25f; // distance at which volume becomes 0
        public bool IsPlaying { get; internal set; }

        private string? _clipPath;

        public void SetClip(string path, bool loop = false)
        {
            _clipPath = path;
            Loop = loop;
            Provider?.Dispose();
            Provider = null;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    Provider = new SpatialAudioSourceProvider(path, loop)
                    {
                        BaseVolume = Volume
                    };
                }
                catch (Exception)
                {
                    // TODO: log error
                }
            }
        }

        public override void OnAwake()
        {
            if (PlayOnAwake && Provider != null)
            {
                Play();
            }
        }

        public void Play()
        {
            if (Provider == null && !string.IsNullOrEmpty(_clipPath))
            {
                SetClip(_clipPath, Loop);
            }
            if (Provider == null) return;
            Provider.BaseVolume = Volume;
            Provider.Loop = Loop;
            Provider.Reset();
            IsPlaying = true;
            AudioEngine.Instance.Register(this);
        }

        public void Stop()
        {
            if (Provider != null)
            {
                Provider.Finished = true; // will output silence
            }
            IsPlaying = false;
            AudioEngine.Instance.Unregister(this);
        }

        internal void MarkStopped()
        {
            IsPlaying = false;
        }

        public override void OnDisable()
        {
            Stop();
        }

        public void Dispose()
        {
            Stop();
            Provider?.Dispose();
        }
    }
}

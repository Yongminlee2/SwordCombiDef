using System.Collections.Generic;
using UnityEngine;

namespace Swordstellation.Runtime
{
    public sealed class AudioService : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        public static AudioService Instance { get; private set; }

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = 0.28f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.72f;
            PlayMusic("forge_ambient");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayMusic(string clipId)
        {
            AudioClip clip = LoadClip(clipId);
            if (clip == null || _musicSource.clip == clip)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void PlaySfx(string clipId, float volume = 1f)
        {
            AudioClip clip = LoadClip(clipId);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        public void SetVolumes(float musicVolume, float sfxVolume)
        {
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.Clamp01(musicVolume);
            }
            if (_sfxSource != null)
            {
                _sfxSource.volume = Mathf.Clamp01(sfxVolume);
            }
        }

        public static void PlayUiClick()
        {
            Instance?.PlaySfx("ui_click", 0.55f);
        }

        private AudioClip LoadClip(string clipId)
        {
            if (_clips.TryGetValue(clipId, out AudioClip clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>($"Audio/{clipId}");
            if (clip == null && clipId == "hit")
            {
                clip = CreateHitClip();
            }
            _clips[clipId] = clip;
            return clip;
        }

        private static AudioClip CreateHitClip()
        {
            const int sampleRate = 44100;
            const int sampleCount = 2205;
            float[] samples = new float[sampleCount];
            uint noise = 0x5EED1234;
            for (int index = 0;
                 index < sampleCount;
                 index++)
            {
                float progress =
                    index / (float)sampleCount;
                float envelope =
                    Mathf.Pow(1f - progress, 3f);
                noise = noise * 1664525u + 1013904223u;
                float random =
                    ((noise >> 8) & 0xFFFF) /
                    32767.5f -
                    1f;
                float metallic =
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        (1100f - progress * 480f) *
                        index /
                        sampleRate);
                samples[index] =
                    (metallic * 0.68f +
                     random * 0.20f) *
                    envelope;
            }

            AudioClip clip = AudioClip.Create(
                "procedural_hit",
                sampleCount,
                1,
                sampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

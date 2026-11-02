using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonUnity
{
    /// Owns the music, ambience and one-shot SFX sources. Volumes come from GameSettings (PlayerPrefs).
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Serializable] public class NamedClip { public string name; public AudioClip[] variants; [Range(0f, 1f)] public float volume = 1f; [Range(0f, 0.5f)] public float pitchJitter = 0.05f; }

        [Header("Sources (created if empty)")]
        public AudioSource musicSource;
        public AudioSource ambienceSource;
        public AudioSource sfxSource;

        [Header("Clips")]
        public AudioClip music;
        public AudioClip ambience;
        public NamedClip[] sfx = Array.Empty<NamedClip>();

        [Header("Behaviour")]
        public bool playMusicOnStart = true;
        public bool playAmbienceOnStart = true;
        [Range(0f, 5f)] public float musicFadeSeconds = 1.5f;
        [Tooltip("Optional preset for default volumes.")] public DungeonConfig config;

        readonly Dictionary<string, NamedClip> lookup = new Dictionary<string, NamedClip>(StringComparer.OrdinalIgnoreCase);
        float musicTarget = 1f, musicFade = 1f;
        public bool MusicOn => musicTarget > 0f;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(this); return; }
            Instance = this;
            musicSource = Ensure(musicSource, "Music", true);
            ambienceSource = Ensure(ambienceSource, "Ambience", true);
            sfxSource = Ensure(sfxSource, "SFX", false);
            foreach (var c in sfx) if (c != null && !string.IsNullOrEmpty(c.name)) lookup[c.name] = c;
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDestroy() { GameSettings.Changed -= Apply; if (Instance == this) Instance = null; }

        void Start()
        {
            if (music && musicSource) { musicSource.clip = music; if (playMusicOnStart) { musicFade = 0f; musicSource.Play(); } }
            if (ambience && ambienceSource) { ambienceSource.clip = ambience; if (playAmbienceOnStart) ambienceSource.Play(); }
        }

        AudioSource Ensure(AudioSource s, string n, bool loop)
        {
            if (s) return s;
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            s = go.AddComponent<AudioSource>(); s.loop = loop; s.playOnAwake = false; s.spatialBlend = 0f;
            return s;
        }

        void Update()
        {
            if (!musicSource) return;
            float step = musicFadeSeconds > 0f ? Time.unscaledDeltaTime / musicFadeSeconds : 1f;
            musicFade = Mathf.MoveTowards(musicFade, musicTarget, step);
            musicSource.volume = GameSettings.Music * musicFade;
            if (musicFade <= 0f && musicSource.isPlaying) musicSource.Pause();
        }

        public void Apply()
        {
            AudioListener.volume = GameSettings.Master;
            if (ambienceSource) ambienceSource.volume = GameSettings.Ambience;
            if (sfxSource) sfxSource.volume = GameSettings.Sfx;
        }

        public void SetMusic(bool on)
        {
            musicTarget = on ? 1f : 0f;
            if (on && musicSource && musicSource.clip && !musicSource.isPlaying) musicSource.UnPause();
            if (on && musicSource && musicSource.clip && !musicSource.isPlaying) musicSource.Play();
        }

        public void ToggleMusic() => SetMusic(!MusicOn);

        /// Plays a named SFX. If position is given, plays it in 3D at that point.
        public static void Play(string name, Vector3? position = null, float volumeScale = 1f)
        {
            var m = Instance; if (!m || string.IsNullOrEmpty(name)) return;
            if (!m.lookup.TryGetValue(name, out var c) || c.variants == null || c.variants.Length == 0) return;
            var clip = c.variants[UnityEngine.Random.Range(0, c.variants.Length)];
            if (!clip) return;
            float vol = c.volume * volumeScale * GameSettings.Sfx;
            if (position.HasValue) PlayAt(clip, position.Value, vol, 1f + UnityEngine.Random.Range(-c.pitchJitter, c.pitchJitter));
            else if (m.sfxSource) { m.sfxSource.pitch = 1f + UnityEngine.Random.Range(-c.pitchJitter, c.pitchJitter); m.sfxSource.PlayOneShot(clip, c.volume * volumeScale); }
        }

        static void PlayAt(AudioClip clip, Vector3 pos, float vol, float pitch)
        {
            var go = new GameObject("OneShot_" + clip.name); go.transform.position = pos;
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.volume = vol; s.pitch = pitch; s.spatialBlend = 1f; s.minDistance = 2f; s.maxDistance = 25f;
            s.Play(); Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }
    }
}

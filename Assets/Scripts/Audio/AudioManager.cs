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
        [Range(0f, 10f), Tooltip("Default crossfade time when a RoomZone changes music or ambience.")] public float crossfadeSeconds = 2.5f;
        [Tooltip("Keep the beat position when crossfading between loops of the same tempo.")] public bool keepBeatOnCrossfade = true;
        [Tooltip("Optional preset for default volumes.")] public DungeonConfig config;

        readonly Dictionary<string, NamedClip> lookup = new Dictionary<string, NamedClip>(StringComparer.OrdinalIgnoreCase);
        float musicTarget = 1f, musicFade = 1f;
        AudioSource musicOld, ambOld; float musicXf = 1f, ambXf = 1f, musicXfDur = 1f, ambXfDur = 1f;
        public AudioClip CurrentMusic => musicSource ? musicSource.clip : null;
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
            musicXf = Mathf.MoveTowards(musicXf, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, musicXfDur));
            ambXf = Mathf.MoveTowards(ambXf, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, ambXfDur));
            musicSource.volume = GameSettings.Music * musicFade * Mathf.Sqrt(musicXf);
            if (musicOld) { musicOld.volume = GameSettings.Music * musicFade * Mathf.Sqrt(1f - musicXf); if (musicXf >= 1f) { musicOld.Stop(); musicOld = null; } }
            if (ambienceSource) ambienceSource.volume = GameSettings.Ambience * Mathf.Sqrt(ambXf);
            if (ambOld) { ambOld.volume = GameSettings.Ambience * Mathf.Sqrt(1f - ambXf); if (ambXf >= 1f) { ambOld.Stop(); ambOld = null; } }
            if (musicFade <= 0f && musicSource.isPlaying) musicSource.Pause();
        }

        public void Apply()
        {
            AudioListener.volume = GameSettings.Master;
            if (sfxSource) sfxSource.volume = GameSettings.Sfx;
        }

        public void SetMusic(bool on)
        {
            musicTarget = on ? 1f : 0f;
            if (on && musicSource && musicSource.clip && !musicSource.isPlaying) musicSource.UnPause();
            if (on && musicSource && musicSource.clip && !musicSource.isPlaying) musicSource.Play();
        }

        public void ToggleMusic() => SetMusic(!MusicOn);

        /// Crossfades the music loop to another clip (null = silence). seconds < 0 uses crossfadeSeconds.
        public void CrossfadeMusic(AudioClip clip, float seconds = -1f)
        {
            if (!musicSource || musicSource.clip == clip) return;
            var next = Spare(musicSource, ref musicOld, "MusicB");
            float time = keepBeatOnCrossfade && musicSource.clip && clip && musicSource.isPlaying ? musicSource.time % clip.length : 0f;
            musicOld = musicSource; musicSource = next; musicXf = 0f; musicXfDur = seconds < 0f ? crossfadeSeconds : seconds;
            musicSource.clip = clip; musicSource.volume = 0f;
            if (clip && MusicOn) { musicSource.time = time; musicSource.Play(); }
        }

        /// Crossfades the ambience loop to another clip (null = silence).
        public void CrossfadeAmbience(AudioClip clip, float seconds = -1f)
        {
            if (!ambienceSource || ambienceSource.clip == clip) return;
            var next = Spare(ambienceSource, ref ambOld, "AmbienceB");
            ambOld = ambienceSource; ambienceSource = next; ambXf = 0f; ambXfDur = seconds < 0f ? crossfadeSeconds : seconds;
            ambienceSource.clip = clip; ambienceSource.volume = 0f;
            if (clip) ambienceSource.Play();
        }

        AudioSource Spare(AudioSource current, ref AudioSource old, string n)
        {
            if (old) { old.Stop(); var o = old; old = null; return o; }
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.outputAudioMixerGroup = current.outputAudioMixerGroup;
            return s;
        }

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

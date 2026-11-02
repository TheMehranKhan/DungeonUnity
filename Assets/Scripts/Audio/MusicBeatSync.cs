using System;
using UnityEngine;
using UnityEngine.Events;

namespace DungeonUnity
{
    /// Analyses the music's spectrum to detect kicks and exposes a 0..1 beat pulse that
    /// lights, lasers and speakers read. Falls back to a steady BPM clock when nothing plays.
    [DisallowMultipleComponent]
    public class MusicBeatSync : MonoBehaviour
    {
        public static MusicBeatSync Instance { get; private set; }
        public enum Source { Spectrum, BpmClock, Both }

        [Header("Input")]
        [Tooltip("Music to analyse. Leave empty to use AudioManager's music source.")] public AudioSource source;
        public Source mode = Source.Both;
        [Range(60f, 200f)] public float bpm = 128f;

        [Header("Spectrum")]
        public FFTWindow window = FFTWindow.BlackmanHarris;
        [Range(1, 32), Tooltip("Low FFT bins summed as 'bass' (of 512).")] public int bassBins = 6;
        [Range(1f, 200f)] public float sensitivity = 40f;
        [Range(1f, 3f), Tooltip("A beat fires when bass exceeds the running average by this factor.")] public float threshold = 1.4f;
        [Range(0.05f, 1f), Tooltip("Minimum seconds between detected beats.")] public float minBeatInterval = 0.25f;

        [Header("Output shaping")]
        [Range(1f, 30f)] public float decay = 8f;
        [Tooltip("Remaps raw pulse (0..1) to output.")] public AnimationCurve response = AnimationCurve.Linear(0, 0, 1, 1);
        public UnityEvent onBeat;
        public event Action Beat;

        /// Smoothed bass level 0..1.
        public float Level { get; private set; }
        /// Pulse that jumps to 1 on each beat then decays.
        public float Pulse { get; private set; }
        public int BeatCount { get; private set; }

        readonly float[] spectrum = new float[512];
        float avg, lastBeat = -10f, clockPhase;

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        bool followManager;

        void Start() { followManager = !source; }

        void Update()
        {
            // Follow the AudioManager's active music source (it swaps during room crossfades).
            if (followManager && AudioManager.Instance) source = AudioManager.Instance.musicSource;
            bool fired = false;
            float bass = 0f;
            bool playing = source && source.isPlaying;
            if (playing && mode != Source.BpmClock)
            {
                source.GetSpectrumData(spectrum, 0, window);
                for (int i = 0; i < Mathf.Min(bassBins, spectrum.Length); i++) bass += spectrum[i];
                bass = Mathf.Clamp01(bass * sensitivity);
                avg = Mathf.Lerp(avg, bass, Time.deltaTime * 2f);
                if (bass > avg * threshold && bass > 0.1f && Time.time - lastBeat > minBeatInterval) fired = true;
            }
            if (!playing || mode != Source.Spectrum)
            {
                float prev = clockPhase;
                clockPhase = Time.time * bpm / 60f;
                if (Mathf.Floor(clockPhase) != Mathf.Floor(prev) && (!playing || mode == Source.BpmClock)) fired = true;
                if (!playing) bass = Mathf.Pow(1f - (clockPhase % 1f), 4f);
            }
            Level = Mathf.Lerp(Level, bass, Time.deltaTime * 12f);
            if (fired)
            {
                lastBeat = Time.time; BeatCount++; Pulse = 1f;
                onBeat?.Invoke(); Beat?.Invoke();
            }
            else Pulse = Mathf.MoveTowards(Pulse, 0f, Time.deltaTime * decay * Pulse + Time.deltaTime);
        }

        /// Beat pulse 0..1 from the active analyser, or a BPM clock if there is none.
        public static float GetPulse(float fallbackBpm = 128f)
        {
            if (Instance && Instance.isActiveAndEnabled) return Instance.response != null ? Instance.response.Evaluate(Instance.Pulse) : Instance.Pulse;
            return Mathf.Pow(1f - (Time.time * fallbackBpm / 60f % 1f), 3f);
        }

        public static float GetLevel(float fallbackBpm = 128f)
        {
            if (Instance && Instance.isActiveAndEnabled) return Instance.Level;
            return Mathf.Pow(1f - (Time.time * fallbackBpm / 60f % 1f), 4f);
        }
    }
}

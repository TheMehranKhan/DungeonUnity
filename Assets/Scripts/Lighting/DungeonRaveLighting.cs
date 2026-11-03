using UnityEngine;

namespace DungeonUnity
{
    /// Room ambient light plus beat-synced colour cycling across a set of lights.
    public class DungeonRaveLighting : MonoBehaviour
    {
        public enum ColorMode { Cycle, Chase, RandomOnBeat, Solid }

        [Header("Lights")]
        public Light[] raveLights = new Light[0];
        [Tooltip("Optional preset; its first room's palette is used at start.")] public DungeonConfig config;
        [Tooltip("Room name looked up in the config at start.")] public string startRoom = "Main Rave Hall";

        [Header("Colour")]
        public ColorMode colorMode = ColorMode.Cycle;
        public Gradient palette = DungeonConfig.DefaultGradient();
        [Range(0f, 2f), Tooltip("Palette cycles per second.")] public float cycleSpeed = 0.15f;

        [Header("Intensity")]
        [Range(60f, 200f)] public float bpm = 128f;
        [Range(0f, 8f)] public float minIntensity = 0.3f;
        [Range(0f, 8f)] public float maxIntensity = 4f;
        [Tooltip("Pulse (0..1) to intensity blend.")] public AnimationCurve pulseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool syncToMusic = true;

        [Header("Atmosphere")]
        public bool applyAmbient = true;
        public Color ambient = new Color(0.03f, 0.02f, 0.05f);
        [Range(0f, 10f), Tooltip("Multiplies every room's ambient colour (DungeonLightingConfig sets this).")] public float ambientBoost = 1f;
        [Tooltip("Use a trilight gradient (brighter ceiling, darker floor) instead of flat ambient.")] public bool trilight = true;
        [Range(0.1f, 10f), Tooltip("Seconds to blend when a room palette changes.")] public float roomBlendSeconds = 1.5f;

        Color[] randomColors;
        int lastBeat = -1;
        Color ambientFrom; float blendT = 1f;

        void OnValidate() { if (maxIntensity < minIntensity) maxIntensity = minIntensity; }

        void Start()
        {
            if (config) { bpm = config.bpm; var r = config.GetRoom(startRoom); if (r != null) ApplyRoom(r, true); }
            if (palette == null || palette.colorKeys.Length < 2) palette = DungeonConfig.DefaultGradient();
            if (raveLights == null) raveLights = new Light[0];
            randomColors = new Color[raveLights.Length];
            ApplyAtmosphere(1f);
        }

        public void ApplyRoom(RoomPalette r, bool instant = false)
        {
            if (r == null) return;
            ambientFrom = RenderSettings.ambientLight;
            palette = r.lightColors ?? palette; ambient = r.ambient; maxIntensity = r.maxIntensity;
            blendT = instant ? 1f : 0f;
        }

        void ApplyAtmosphere(float k)
        {
            if (applyAmbient)
            {
                var c = Color.Lerp(ambientFrom, ambient * ambientBoost, k);
                if (trilight)
                {
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = c * 1.4f; RenderSettings.ambientEquatorColor = c; RenderSettings.ambientGroundColor = c * 0.5f;
                }
                else RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = c;
            }
            RenderSettings.fog = false; // fog/smoke removed
        }

        void Update()
        {
            if (blendT < 1f) { blendT = Mathf.Min(1f, blendT + Time.deltaTime / roomBlendSeconds); ApplyAtmosphere(blendT); }
            if (raveLights == null || raveLights.Length == 0) return;
            float pulse = syncToMusic ? MusicBeatSync.GetPulse(bpm) : Mathf.Pow(1f - (Time.time * bpm / 60f % 1f), 3f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulseCurve.Evaluate(pulse));
            int beat = MusicBeatSync.Instance ? MusicBeatSync.Instance.BeatCount : Mathf.FloorToInt(Time.time * bpm / 60f);
            bool newBeat = beat != lastBeat; lastBeat = beat;
            if (randomColors == null || randomColors.Length != raveLights.Length) randomColors = new Color[raveLights.Length];
            for (int i = 0; i < raveLights.Length; i++)
            {
                var l = raveLights[i]; if (!l) continue;
                float offset = i / (float)Mathf.Max(1, raveLights.Length);
                Color c;
                switch (colorMode)
                {
                    case ColorMode.Chase: c = palette.Evaluate(Mathf.Repeat(offset + beat * 0.125f, 1f)); break;
                    case ColorMode.RandomOnBeat: if (newBeat) randomColors[i] = palette.Evaluate(Random.value); c = randomColors[i]; break;
                    case ColorMode.Solid: c = palette.Evaluate(0f); break;
                    default: c = palette.Evaluate(Mathf.Repeat(Time.time * cycleSpeed + offset, 1f)); break;
                }
                l.color = c; l.intensity = intensity;
            }
        }
    }
}

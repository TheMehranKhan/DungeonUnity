using UnityEngine;

namespace DungeonUnity
{
    /// Red warning / stage lights: strobe, breathe, beacon, or beat flash.
    public class RedLightStrobe : MonoBehaviour
    {
        public enum Mode { Strobe, Breathe, Beacon, BeatFlash }

        [Header("Targets")]
        public Light[] lights = new Light[0];
        [Tooltip("Renderers whose emission follows the lights.")] public Renderer[] emissiveRenderers = new Renderer[0];

        [Header("Look")]
        public Mode mode = Mode.Breathe;
        public Color red = new Color(1f, 0.05f, 0.05f);
        [Range(0f, 10f)] public float intensity = 3f;
        [Range(0f, 10f)] public float emissionBoost = 2f;

        [Header("Timing")]
        [Range(0.5f, 30f)] public float strobeHz = 8f;
        [Range(0.05f, 5f)] public float breatheSpeed = 0.8f;
        [Range(0f, 720f)] public float beaconDegPerSec = 180f;
        [Range(60f, 200f)] public float bpm = 128f;

        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb;

        void Awake() { mpb = new MaterialPropertyBlock(); foreach (var r in emissiveRenderers) if (r) foreach (var m in r.sharedMaterials) if (m) m.EnableKeyword("_EMISSION"); }

        void Update()
        {
            float k;
            switch (mode)
            {
                case Mode.Strobe: k = (Time.time * strobeHz % 1f) < 0.5f ? 1f : 0f; break;
                case Mode.Breathe: k = 0.5f + 0.5f * Mathf.Sin(Time.time * breatheSpeed * Mathf.PI * 2f); break;
                case Mode.BeatFlash: k = MusicBeatSync.GetPulse(bpm); break;
                default: k = 1f; break;
            }
            foreach (var l in lights)
            {
                if (!l) continue;
                l.color = red; l.intensity = intensity * k;
                if (mode == Mode.Beacon) l.transform.Rotate(0f, beaconDegPerSec * Time.deltaTime, 0f, Space.World);
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in emissiveRenderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb); mpb.SetColor(Emission, red * k * emissionBoost); r.SetPropertyBlock(mpb);
            }
        }
    }
}

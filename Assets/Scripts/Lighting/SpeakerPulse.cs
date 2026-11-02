using UnityEngine;

namespace DungeonUnity
{
    /// Pumps speaker cones (or the whole cabinet) with the bass level from MusicBeatSync.
    public class SpeakerPulse : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Transforms to scale; defaults to this object.")] public Transform[] cones = new Transform[0];
        public Light glow;

        [Header("Feel")]
        [Range(0f, 1f)] public float pulseScale = 0.08f;
        [Range(1f, 40f)] public float smoothing = 12f;
        [Range(0f, 10f)] public float glowMax = 3f;
        public Gradient glowColor = DungeonConfig.DefaultGradient();
        [Range(60f, 200f)] public float fallbackBpm = 128f;
        [Tooltip("Squash on Y while stretching XZ (cartoon feel).")] public bool squashAndStretch = false;

        Vector3[] baseScales = new Vector3[0]; float level;

        void Start()
        {
            if (cones == null || cones.Length == 0) cones = new[] { transform };
            baseScales = new Vector3[cones.Length];
            for (int i = 0; i < cones.Length; i++) if (cones[i]) baseScales[i] = cones[i].localScale;
        }

        void Update()
        {
            float target = MusicBeatSync.GetLevel(fallbackBpm);
            level = Mathf.Lerp(level, target, Time.deltaTime * smoothing);
            for (int i = 0; i < cones.Length && i < baseScales.Length; i++)
            {
                if (!cones[i]) continue;
                float k = 1f + level * pulseScale;
                cones[i].localScale = squashAndStretch ? Vector3.Scale(baseScales[i], new Vector3(k, 1f / k, k)) : baseScales[i] * k;
            }
            if (glow) { glow.intensity = level * glowMax; if (glowColor != null) glow.color = glowColor.Evaluate(Mathf.Repeat(Time.time * 0.1f, 1f)); }
        }
    }
}

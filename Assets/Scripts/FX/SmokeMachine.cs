using UnityEngine;

namespace DungeonUnity
{
    /// Configures a low-lying smoke/fog particle system in code; optional bursts on the beat.
    [RequireComponent(typeof(ParticleSystem))]
    public class SmokeMachine : MonoBehaviour
    {
        [Header("Emission")]
        [Range(0f, 200f)] public float rate = 12f;
        [Range(0.5f, 30f)] public float lifetime = 9f;
        [Range(0f, 5f)] public float speed = 0.6f;
        [Range(0.1f, 10f)] public float startSize = 3f;
        public Color color = new Color(0.6f, 0.55f, 0.75f, 0.18f);
        [Range(1, 2000)] public int maxParticles = 400;

        [Header("Area")]
        public Vector3 boxSize = new Vector3(4f, 0.2f, 1f);

        [Header("Beat")]
        public bool burstOnBeat = true;
        [Range(1, 32)] public int burstEveryBeats = 16;
        [Range(1, 200)] public int burstCount = 40;

        ParticleSystem ps; int lastBeat;

        void Awake() { ps = GetComponent<ParticleSystem>(); Configure(); }
        void OnValidate() { if (Application.isPlaying && ps) Configure(); }

        public void Configure()
        {
            if (!ps) ps = GetComponent<ParticleSystem>();
            var main = ps.main; main.startLifetime = lifetime; main.startSpeed = speed; main.startSize = startSize; main.startColor = color; main.maxParticles = maxParticles; main.simulationSpace = ParticleSystemSimulationSpace.World; main.loop = true;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = boxSize;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var r = GetComponent<ParticleSystemRenderer>();
            if (r && !r.sharedMaterial) { var s = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"); if (s) r.sharedMaterial = new Material(s); }
            if (!ps.isPlaying) ps.Play();
        }

        void Update()
        {
            if (!burstOnBeat || !MusicBeatSync.Instance) return;
            int b = MusicBeatSync.Instance.BeatCount;
            if (b != lastBeat && b % burstEveryBeats == 0) ps.Emit(burstCount);
            lastBeat = b;
        }
    }
}

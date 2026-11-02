using UnityEngine;

namespace DungeonUnity
{
    /// Spawns LineRenderer laser beams from this transform and sweeps them in patterns.
    public class LaserShowController : MonoBehaviour
    {
        public enum Pattern { Fan, Scan, Spin, Strobe, Tunnel }

        [Header("Beams")]
        [Range(1, 64)] public int beamCount = 8;
        [Range(1f, 100f)] public float range = 30f;
        [Range(0.002f, 0.2f)] public float width = 0.02f;
        [ColorUsage(true, true)] public Color color = Color.red;
        [Tooltip("Colour over beam length (alpha fades the tail).")] public Gradient beamFade;
        public Material beamMaterial;
        public LayerMask hitMask = ~0;
        [Tooltip("Stop beams at colliders.")] public bool raycast = true;

        [Header("Motion")]
        public Pattern pattern = Pattern.Fan;
        [Range(0f, 180f)] public float spreadDegrees = 60f;
        [Range(0f, 10f)] public float speed = 1f;
        [Range(0f, 45f)] public float tiltDegrees = 10f;
        public bool autoCyclePatterns = true;
        [Range(1f, 60f)] public float patternSeconds = 8f;

        [Header("Beat")]
        [Range(60f, 200f)] public float bpm = 128f;
        [Tooltip("Widen beams on each beat.")] public bool pulseWidthOnBeat = true;
        [Range(1f, 5f)] public float beatWidthMultiplier = 2f;
        [Tooltip("Named AudioManager SFX played when the pattern changes.")] public string patternChangeSfx = "LaserZap";

        LineRenderer[] beams = new LineRenderer[0];
        Pattern lastPattern;

        void OnValidate() { beamCount = Mathf.Clamp(beamCount, 1, 64); }

        void OnEnable() { if (beams.Length == 0) Build(); SetVisible(true); }
        void OnDisable() { SetVisible(false); }

        void SetVisible(bool on) { foreach (var b in beams) if (b) b.enabled = on; }

        public void Build()
        {
            foreach (var b in beams) if (b) Destroy(b.gameObject);
            beams = new LineRenderer[beamCount];
            var mat = beamMaterial;
            if (!mat) { var sh = Shader.Find("Sprites/Default"); mat = sh ? new Material(sh) : null; }
            if (beamFade == null || beamFade.colorKeys.Length < 2)
            {
                beamFade = new Gradient();
                beamFade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.15f, 1f) });
            }
            for (int i = 0; i < beamCount; i++)
            {
                var go = new GameObject("Beam" + i); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                if (mat) lr.sharedMaterial = mat;
                lr.positionCount = 2; lr.useWorldSpace = true; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beams[i] = lr;
            }
            ApplyColor();
        }

        public void ApplyColor()
        {
            var g = new Gradient();
            var ck = beamFade.colorKeys; for (int i = 0; i < ck.Length; i++) ck[i].color *= color;
            g.SetKeys(ck, beamFade.alphaKeys);
            foreach (var b in beams) if (b) b.colorGradient = g;
        }

        public void SetColor(Color c) { color = c; ApplyColor(); }

        void Update()
        {
            if (beams.Length == 0) return;
            if (autoCyclePatterns) pattern = (Pattern)((int)(Time.time / patternSeconds) % 5);
            if (pattern != lastPattern) { lastPattern = pattern; AudioManager.Play(patternChangeSfx, transform.position, 0.4f); }
            float t = Time.time * speed;
            float pulse = MusicBeatSync.GetPulse(bpm);
            bool on = pattern != Pattern.Strobe || pulse > 0.5f;
            float w = width * (pulseWidthOnBeat ? Mathf.Lerp(1f, beatWidthMultiplier, pulse) : 1f);
            Vector3 origin = transform.position;
            for (int i = 0; i < beams.Length; i++)
            {
                var b = beams[i]; if (!b) continue;
                float f = beams.Length > 1 ? i / (float)(beams.Length - 1) - 0.5f : 0f;
                float yaw = 0f, pitch = 0f;
                switch (pattern)
                {
                    case Pattern.Fan: yaw = f * spreadDegrees + Mathf.Sin(t) * 15f; pitch = Mathf.Sin(t * 0.7f) * tiltDegrees; break;
                    case Pattern.Scan: yaw = Mathf.Sin(t + i * 0.3f) * spreadDegrees * 0.5f; pitch = f * 2f * tiltDegrees; break;
                    case Pattern.Spin: yaw = (t * 90f + i * 360f / beams.Length) % 360f; pitch = tiltDegrees * 1.5f; break;
                    case Pattern.Strobe: yaw = f * spreadDegrees; pitch = tiltDegrees * 0.5f; break;
                    case Pattern.Tunnel: float a = i * Mathf.PI * 2f / beams.Length + t; yaw = Mathf.Cos(a) * spreadDegrees * 0.25f; pitch = Mathf.Sin(a) * spreadDegrees * 0.25f; break;
                }
                Vector3 dir = transform.rotation * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                Vector3 end = origin + dir * range;
                if (raycast && Physics.Raycast(origin, dir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore)) end = hit.point;
                b.enabled = on; b.startWidth = b.endWidth = w;
                b.SetPosition(0, origin); b.SetPosition(1, end);
            }
        }
    }
}

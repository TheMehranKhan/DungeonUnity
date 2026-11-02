using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Camera trip: FOV wobble, roll sway and a hue-cycling screen overlay. Call Trigger() (pills do).
    [RequireComponent(typeof(Camera))]
    public class TrippyEffect : MonoBehaviour
    {
        public static TrippyEffect Instance { get; private set; }

        [Header("Overlay")]
        [Tooltip("Full-screen UI Image tinted by the effect (optional).")] public Image overlay;
        [Range(0f, 1f)] public float overlayMaxAlpha = 0.25f;
        [Range(0f, 2f), Tooltip("Hue cycles per second.")] public float hueSpeed = 0.4f;

        [Header("Camera")]
        [Range(0f, 40f)] public float fovWobble = 12f;
        [Range(0.1f, 10f)] public float wobbleSpeed = 1.3f;
        [Range(0f, 30f)] public float rollDegrees = 6f;
        [Tooltip("Strength over the trip's lifetime (0..1 time).")] public AnimationCurve envelope = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.1f, 1), new Keyframe(0.8f, 1), new Keyframe(1, 0));

        [Header("Post FX (optional)")]
        public PostFXSetup postFX;
        [Range(0f, 180f)] public float postHueRange = 120f;

        [Header("Debug")]
        public bool alwaysOn = false;

        Camera cam; float baseFov, start = -1f, duration = 1f, strength;

        void Awake() { cam = GetComponent<Camera>(); baseFov = cam.fieldOfView; }
        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; Apply(0f); }

        public void Trigger(float seconds = 12f, float intensity = 1f)
        {
            start = Time.time; duration = Mathf.Max(0.5f, seconds); strength = Mathf.Clamp01(intensity);
        }

        public static void TriggerGlobal(float seconds, float intensity) { if (Instance) Instance.Trigger(seconds, intensity); }

        void LateUpdate()
        {
            float k = 0f;
            if (alwaysOn) k = 1f;
            else if (start >= 0f)
            {
                float t = (Time.time - start) / duration;
                if (t >= 1f) start = -1f; else k = envelope.Evaluate(t) * strength;
            }
            Apply(k);
        }

        void Apply(float k)
        {
            if (cam) cam.fieldOfView = baseFov + Mathf.Sin(Time.time * wobbleSpeed * Mathf.PI * 2f) * fovWobble * k;
            if (rollDegrees > 0f)
            {
                var e = transform.localEulerAngles;
                transform.localEulerAngles = new Vector3(e.x, e.y, Mathf.Sin(Time.time * wobbleSpeed * 0.7f) * rollDegrees * k);
            }
            float hue = Mathf.Repeat(Time.time * hueSpeed, 1f);
            if (overlay)
            {
                var c = Color.HSVToRGB(hue, 0.9f, 1f); c.a = overlayMaxAlpha * k;
                overlay.color = c; overlay.enabled = k > 0.001f;
            }
            if (postFX) postFX.SetTrip((hue * 2f - 1f) * postHueRange * k, 0.6f * k);
        }

        public void SetBaseFov(float fov) { baseFov = fov; }
    }
}

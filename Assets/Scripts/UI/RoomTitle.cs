using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Fades the current room's name in and out on the HUD when the player changes room.
    public class RoomTitle : MonoBehaviour
    {
        public Text label;
        [Range(0.1f, 10f)] public float holdSeconds = 2f;
        [Range(0.05f, 3f)] public float fadeSeconds = 0.6f;
        public Color color = new Color(1f, 0.4f, 0.9f);
        [Tooltip("Upper-case the room name.")] public bool upperCase = true;
        float shownAt = -100f;

        void OnEnable() { RoomZone.Changed += Show; if (label) label.text = ""; }
        void OnDisable() { RoomZone.Changed -= Show; }

        void Show(RoomZone z) { if (!label || !z) return; label.text = upperCase ? z.Title.ToUpperInvariant() : z.Title; shownAt = Time.time; }

        void Update()
        {
            if (!label) return;
            float t = Time.time - shownAt;
            float a = t < fadeSeconds ? t / fadeSeconds : t < fadeSeconds + holdSeconds ? 1f : 1f - (t - fadeSeconds - holdSeconds) / fadeSeconds;
            var c = color; c.a = Mathf.Clamp01(a) * color.a; label.color = c;
        }
    }
}

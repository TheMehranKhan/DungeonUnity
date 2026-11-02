using UnityEngine;

namespace DungeonUnity
{
    /// Flickering, buzzing fluorescent bathroom lights with occasional blackouts.
    public class BathroomLights : MonoBehaviour
    {
        [Header("Targets")]
        public Light[] lights = new Light[0];
        [Tooltip("Looping buzz/room tone; muted during blackouts.")] public AudioSource buzz;

        [Header("Look")]
        public Color tint = new Color(0.75f, 1f, 0.85f);
        [Range(0f, 5f)] public float baseIntensity = 1.2f;

        [Header("Flicker")]
        public bool flicker = true;
        [Range(0f, 1f)] public float flickerChance = 0.08f;
        [Range(0f, 1f)] public float flickerMin = 0.05f;
        public bool blackouts = true;
        [Range(0f, 0.05f)] public float blackoutChance = 0.003f;
        [Range(0.1f, 5f)] public float blackoutSeconds = 1.5f;
        [Range(0f, 1f)] public float buzzVolume = 0.25f;

        float blackoutUntil;

        void Start() { foreach (var l in lights) if (l) l.color = tint; }

        void Update()
        {
            bool blackout = Time.time < blackoutUntil;
            if (blackouts && !blackout && Random.value < blackoutChance) blackoutUntil = Time.time + Random.Range(0.3f, blackoutSeconds);
            foreach (var l in lights)
            {
                if (!l) continue;
                float i = blackout ? 0f : baseIntensity;
                if (flicker && !blackout && Random.value < flickerChance) i *= Random.Range(flickerMin, 0.6f);
                l.intensity = i;
            }
            if (buzz) buzz.volume = blackout ? 0f : buzzVolume * GameSettings.Ambience;
        }
    }
}

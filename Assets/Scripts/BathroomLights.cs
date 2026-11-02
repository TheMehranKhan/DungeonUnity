using UnityEngine;

/// Flickering, buzzing fluorescent bathroom lights with occasional blackouts.
public class BathroomLights : MonoBehaviour
{
    public Light[] lights; public AudioSource buzz; public Color tint = new Color(0.75f, 1f, 0.85f);
    public float baseIntensity = 1.2f, flickerChance = 0.08f, blackoutChance = 0.003f, blackoutSeconds = 1.5f;
    float blackoutUntil;

    void Start() { foreach (var l in lights) if (l) l.color = tint; }

    void Update()
    {
        bool blackout = Time.time < blackoutUntil;
        if (!blackout && Random.value < blackoutChance) blackoutUntil = Time.time + Random.Range(0.3f, blackoutSeconds);
        foreach (var l in lights)
        {
            if (!l) continue;
            float i = blackout ? 0f : baseIntensity;
            if (!blackout && Random.value < flickerChance) i *= Random.Range(0.05f, 0.6f);
            l.intensity = i;
        }
        if (buzz) buzz.volume = blackout ? 0f : 0.25f;
    }
}

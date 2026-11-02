using UnityEngine;

/// Dark dungeon ambience plus beat-synced rave color cycling across a set of lights.
public class DungeonRaveLighting : MonoBehaviour
{
    public Light[] raveLights;
    public Gradient palette;
    public float bpm = 128f, minIntensity = 0.3f, maxIntensity = 4f, cycleSpeed = 0.15f;
    public Color ambient = new Color(0.03f, 0.02f, 0.05f);
    public bool fog = true; public Color fogColor = new Color(0.05f, 0.02f, 0.08f); public float fogDensity = 0.04f;

    void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fog = fog; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = fogDensity;
        if (palette == null || palette.colorKeys.Length < 2)
        {
            palette = new Gradient();
            palette.SetKeys(new[] { new GradientColorKey(Color.magenta, 0f), new GradientColorKey(Color.cyan, 0.33f), new GradientColorKey(Color.red, 0.66f), new GradientColorKey(Color.magenta, 1f) },
                            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }
    }

    void Update()
    {
        float beat = Time.time * bpm / 60f;
        float pulse = Mathf.Pow(1f - (beat % 1f), 3f);
        for (int i = 0; i < raveLights.Length; i++)
        {
            var l = raveLights[i]; if (!l) continue;
            float t = Mathf.Repeat(Time.time * cycleSpeed + i / (float)Mathf.Max(1, raveLights.Length), 1f);
            l.color = palette.Evaluate(t);
            l.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }
    }
}

using UnityEngine;

/// Red warning / stage lights: strobe, slow breathe, or rotating beacon.
public class RedLightStrobe : MonoBehaviour
{
    public enum Mode { Strobe, Breathe, Beacon }
    public Mode mode = Mode.Breathe; public Light[] lights; public Color red = new Color(1f, 0.05f, 0.05f);
    public float intensity = 3f, strobeHz = 8f, breatheSpeed = 0.8f, beaconDegPerSec = 180f;
    public Renderer[] emissiveRenderers;
    static readonly int Emission = Shader.PropertyToID("_EmissionColor");

    void Update()
    {
        float k = mode switch
        {
            Mode.Strobe => (Time.time * strobeHz % 1f) < 0.5f ? 1f : 0f,
            Mode.Breathe => 0.5f + 0.5f * Mathf.Sin(Time.time * breatheSpeed * Mathf.PI * 2f),
            _ => 1f
        };
        foreach (var l in lights)
        {
            if (!l) continue;
            l.color = red; l.intensity = intensity * k;
            if (mode == Mode.Beacon) l.transform.Rotate(0f, beaconDegPerSec * Time.deltaTime, 0f, Space.World);
        }
        foreach (var r in emissiveRenderers) if (r) { r.material.EnableKeyword("_EMISSION"); r.material.SetColor(Emission, red * k * 2f); }
    }
}

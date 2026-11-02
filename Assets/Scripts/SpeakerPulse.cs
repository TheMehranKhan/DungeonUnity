using UnityEngine;

/// Makes speaker cones (or the whole cabinet) pump to the music using spectrum data, with a BPM fallback.
public class SpeakerPulse : MonoBehaviour
{
    public AudioSource source; public Transform[] cones; public float pulseScale = 0.15f, sensitivity = 40f, smoothing = 12f;
    public float fallbackBpm = 128f; public Light glow; public float glowMax = 3f;
    readonly float[] spectrum = new float[256]; Vector3[] baseScales; float level;

    void Start() { baseScales = new Vector3[cones.Length]; for (int i = 0; i < cones.Length; i++) baseScales[i] = cones[i].localScale; }

    void Update()
    {
        float target;
        if (source && source.isPlaying)
        {
            source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            float bass = 0f; for (int i = 0; i < 8; i++) bass += spectrum[i];
            target = Mathf.Clamp01(bass * sensitivity);
        }
        else target = Mathf.Pow(1f - (Time.time * fallbackBpm / 60f % 1f), 4f);
        level = Mathf.Lerp(level, target, Time.deltaTime * smoothing);
        for (int i = 0; i < cones.Length; i++) if (cones[i]) cones[i].localScale = baseScales[i] * (1f + level * pulseScale);
        if (glow) glow.intensity = level * glowMax;
    }
}

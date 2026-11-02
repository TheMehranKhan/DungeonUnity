using UnityEngine;

/// Spawns LineRenderer laser beams from an emitter and sweeps them in fan / scan patterns.
public class LaserShowController : MonoBehaviour
{
    public enum Pattern { Fan, Scan, Spin, Strobe }
    public Pattern pattern = Pattern.Fan;
    public int beamCount = 8; public float range = 30f, spreadDegrees = 60f, speed = 1f, width = 0.02f;
    public Color color = Color.red; public Material beamMaterial; public LayerMask hitMask = ~0;
    public float bpm = 128f; public bool autoCyclePatterns = true; public float patternSeconds = 8f;
    LineRenderer[] beams;

    void Start()
    {
        beams = new LineRenderer[beamCount];
        var mat = beamMaterial ? beamMaterial : new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < beamCount; i++)
        {
            var go = new GameObject("Beam" + i); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = mat; lr.startWidth = lr.endWidth = width; lr.positionCount = 2; lr.useWorldSpace = true;
            lr.startColor = color; lr.endColor = new Color(color.r, color.g, color.b, 0.2f);
            beams[i] = lr;
        }
    }

    void Update()
    {
        if (autoCyclePatterns) pattern = (Pattern)((int)(Time.time / patternSeconds) % 4);
        float t = Time.time * speed;
        bool on = pattern != Pattern.Strobe || (Time.time * bpm / 60f * 2f) % 1f < 0.5f;
        for (int i = 0; i < beams.Length; i++)
        {
            float f = beams.Length > 1 ? i / (float)(beams.Length - 1) - 0.5f : 0f;
            float yaw = 0f, pitch = 0f;
            switch (pattern)
            {
                case Pattern.Fan: yaw = f * spreadDegrees + Mathf.Sin(t) * 15f; pitch = Mathf.Sin(t * 0.7f) * 10f; break;
                case Pattern.Scan: yaw = Mathf.Sin(t + i * 0.3f) * spreadDegrees * 0.5f; pitch = f * 20f; break;
                case Pattern.Spin: yaw = (t * 90f + i * 360f / beams.Length) % 360f; pitch = 15f; break;
                case Pattern.Strobe: yaw = f * spreadDegrees; pitch = 5f; break;
            }
            Vector3 dir = transform.rotation * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            Vector3 end = transform.position + dir * range;
            if (Physics.Raycast(transform.position, dir, out var hit, range, hitMask)) end = hit.point;
            beams[i].enabled = on;
            beams[i].SetPosition(0, transform.position); beams[i].SetPosition(1, end);
        }
    }
}

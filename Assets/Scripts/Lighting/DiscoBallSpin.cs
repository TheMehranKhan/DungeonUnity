using UnityEngine;

namespace DungeonUnity
{
    /// Spins a disco ball and sweeps coloured spot lights around the room with it.
    public class DiscoBallSpin : MonoBehaviour
    {
        [Header("Spin")]
        [Range(0f, 360f)] public float degreesPerSecond = 30f;
        public Vector3 axis = Vector3.up;
        [Range(0f, 3f), Tooltip("Extra spin kick on each beat.")] public float beatKick = 0.5f;

        [Header("Light spots")]
        public bool createSpots = true;
        [Range(0, 24)] public int spotCount = 8;
        [Range(1f, 120f)] public float spotAngle = 12f;
        [Range(0f, 10f)] public float spotIntensity = 2.5f;
        [Range(1f, 50f)] public float spotRange = 18f;
        public Gradient spotColors = DungeonConfig.DefaultGradient();

        Light[] spots = new Light[0];

        void OnValidate() { if (axis == Vector3.zero) axis = Vector3.up; }

        void Start()
        {
            if (!createSpots || spotCount <= 0) return;
            var pivot = new GameObject("Spots").transform; pivot.SetParent(transform, false);
            spots = new Light[spotCount];
            for (int i = 0; i < spotCount; i++)
            {
                var go = new GameObject("Spot" + i); go.transform.SetParent(pivot, false);
                go.transform.localRotation = Quaternion.Euler(Random.Range(20f, 80f), i * 360f / spotCount, 0f);
                var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.spotAngle = spotAngle; l.range = spotRange; l.intensity = spotIntensity; l.shadows = LightShadows.None;
                l.color = spotColors.Evaluate(i / (float)spotCount); spots[i] = l;
            }
        }

        void Update()
        {
            float speed = degreesPerSecond * (1f + MusicBeatSync.GetPulse() * beatKick);
            transform.Rotate(axis.normalized, speed * Time.deltaTime, Space.World);
            for (int i = 0; i < spots.Length; i++) if (spots[i]) spots[i].color = spotColors.Evaluate(Mathf.Repeat(i / (float)spots.Length + Time.time * 0.05f, 1f));
        }
    }
}

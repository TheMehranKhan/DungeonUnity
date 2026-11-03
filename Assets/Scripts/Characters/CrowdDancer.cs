using UnityEngine;

namespace DungeonUnity
{
    /// Makes a crowd member dance to the beat: bob, sway, twist, jump or headbang, each with a random phase.
    public class CrowdDancer : MonoBehaviour
    {
        public enum DanceStyle { Bob, Sway, Twist, Jump, Headbang, Groove, StepTouch, Random }

        public DanceStyle style = DanceStyle.Random;
        [Range(0f, 0.5f)] public float bobHeight = 0.08f;
        [Range(0f, 30f)] public float swayDegrees = 8f;
        [Range(0f, 90f)] public float twistDegrees = 25f;
        [Range(0f, 1f)] public float jumpHeight = 0.25f;
        [Range(0f, 40f)] public float headbangDegrees = 15f;
        [Range(60f, 200f), Tooltip("Fallback BPM.")] public float bpm = 128f;
        [Range(0.25f, 4f), Tooltip("Beats per move.")] public float beatsPerMove = 1f;
        [Tooltip("Randomise phase and strength so the crowd isn't in lockstep.")] public bool randomize = true;
        [Range(0f, 1f)] public float strengthJitter = 0.3f;
        [Range(0, 16), Tooltip("Random style only: pick a new move every N bars (0 = keep one).")] public int switchEveryBars = 4;
        [Range(0f, 0.4f)] public float stepDistance = 0.12f;

        Vector3 basePos; Quaternion baseRot; float phase, strength = 1f; DanceStyle active; int lastBlock = -1;

        void Start()
        {
            basePos = transform.localPosition; baseRot = transform.localRotation;
            active = style == DanceStyle.Random ? (DanceStyle)Random.Range(0, 7) : style;
            if (randomize) { phase = Random.value; strength = 1f + Random.Range(-strengthJitter, strengthJitter); }
        }

        void Update()
        {
            float beatT = Time.time * bpm / 60f / beatsPerMove + phase;
            float s = Mathf.Sin(beatT * Mathf.PI * 2f);
            float a = Mathf.Abs(s);
            float pulse = MusicBeatSync.GetPulse(bpm);
            if (style == DanceStyle.Random && switchEveryBars > 0)
            {
                int block = Mathf.FloorToInt(Time.timeSinceLevelLoad * bpm / 60f / 4f / switchEveryBars);
                if (block != lastBlock) { lastBlock = block; active = (DanceStyle)Random.Range(0, 7); }
            }
            Vector3 p = basePos; Quaternion r = baseRot;
            switch (active)
            {
                case DanceStyle.Bob: p += Vector3.up * a * bobHeight * strength * (1f + pulse); break;
                case DanceStyle.Sway: r *= Quaternion.Euler(0f, 0f, s * swayDegrees * strength); p += Vector3.up * a * bobHeight * 0.5f; break;
                case DanceStyle.Twist: r *= Quaternion.Euler(0f, s * twistDegrees * strength, 0f); break;
                case DanceStyle.Jump: p += Vector3.up * Mathf.Max(0f, s) * jumpHeight * strength; break;
                case DanceStyle.Groove: p += Vector3.up * -a * bobHeight * strength; r *= Quaternion.Euler(Mathf.Pow(1f - Mathf.Repeat(beatT, 1f), 4f) * headbangDegrees * 0.5f, 0f, Mathf.Sin(beatT * Mathf.PI) * swayDegrees * strength); break;
                case DanceStyle.StepTouch: p += baseRot * Vector3.right * Mathf.Sin(beatT * Mathf.PI) * stepDistance * strength; p += Vector3.up * a * bobHeight * 0.5f; r *= Quaternion.Euler(0f, 0f, -Mathf.Sin(beatT * Mathf.PI) * swayDegrees * 0.5f); break;
                case DanceStyle.Headbang: r *= Quaternion.Euler(a * headbangDegrees * strength, 0f, 0f); break;
            }
            transform.localPosition = p; transform.localRotation = r;
        }
    }
}

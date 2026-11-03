using UnityEngine;

namespace DungeonUnity
{
    /// Beat-synced procedural dance for a CharacterRig: bounce, hip sway, head bob (body pitch), body twist and
    /// step-touch on the legs. The move changes every few bars. Dancers sharing a variantSeed pick the same moves,
    /// and mirror flips the side-to-side motion so two dancers facing each other look like a pair.
    /// Turns ProceduralWalk off while it is enabled, and back on when disabled.
    [RequireComponent(typeof(CharacterRig))]
    public class ProceduralDance : MonoBehaviour
    {
        public enum Move { Groove, StepTouch, Twist, Bounce, Rock, Shuffle }

        [Header("Tempo")]
        [Range(60f, 200f), Tooltip("Fallback BPM. Every dancer uses the same clock, so they stay in time with each other.")] public float bpm = 128f;
        [Range(0f, 1f), Tooltip("Beat offset (0..1). Keep 0 for pairs and crowds that should hit together.")] public float phaseOffset = 0f;
        [Range(0f, 2f), Tooltip("Extra bounce on detected beats (MusicBeatSync pulse).")] public float pulseBoost = 0.6f;

        [Header("Moves")]
        [Tooltip("Switch to a new move every N bars (0 = always use startMove).")] [Range(0, 16)] public int switchEveryBars = 4;
        public Move startMove = Move.Groove;
        [Tooltip("Dancers with the same seed pick the same moves at the same time.")] public int variantSeed = 0;
        [Tooltip("Flip side-to-side motion (for a partner facing you).")] public bool mirror = false;
        [Range(0f, 2f)] public float intensity = 1f;

        [Header("Amounts")]
        [Range(0f, 0.3f)] public float bounceHeight = 0.06f;
        [Range(0f, 30f)] public float hipSwayDegrees = 9f;
        [Range(0f, 25f)] public float headBobDegrees = 7f;
        [Range(0f, 60f)] public float twistDegrees = 22f;
        [Range(0f, 0.4f)] public float stepDistance = 0.12f;
        [Range(0f, 45f)] public float legDegrees = 18f;

        public Move CurrentMove { get; private set; }

        CharacterRig rig; ProceduralWalk walk; int cachedVersion = -1; Transform body;
        Vector3 bodyBase; Quaternion bodyRot, lh, rh, lk, rk; float blend;

        void Awake() { rig = GetComponent<CharacterRig>(); walk = GetComponent<ProceduralWalk>(); CurrentMove = startMove; }
        void OnEnable() { if (walk) walk.enabled = false; }
        void OnDisable()
        {
            if (walk) walk.enabled = true;
            if (body) { body.localPosition = bodyBase; body.localRotation = bodyRot; }
        }

        void Cache()
        {
            cachedVersion = rig.Version;
            body = rig.body && rig.body != transform ? rig.body : null;
            if (body) { bodyBase = body.localPosition; bodyRot = body.localRotation; }
            if (rig.LeftHip) lh = rig.LeftHip.localRotation; if (rig.RightHip) rh = rig.RightHip.localRotation;
            if (rig.LeftKnee) lk = rig.LeftKnee.localRotation; if (rig.RightKnee) rk = rig.RightKnee.localRotation;
        }

        /// Shared beat clock in beats.
        public static float BeatTime(float bpm) => Time.timeSinceLevelLoad * bpm / 60f;

        public static Move PickMove(int seed, int block, Move start)
        {
            if (block <= 0) return start;
            unchecked { int h = seed * 73856093 ^ block * 19349663; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (Move)(((h % 6) + 6) % 6); }
        }

        void LateUpdate()
        {
            if (!rig) return;
            if (rig.Version != cachedVersion) Cache();
            blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime * 2f);
            float beats = BeatTime(bpm) + phaseOffset;
            int bar = Mathf.FloorToInt(beats / 4f);
            CurrentMove = switchEveryBars > 0 ? PickMove(variantSeed, bar / switchEveryBars, startMove) : startMove;

            float ph = beats * Mathf.PI * 2f;          // one cycle per beat
            float half = beats * Mathf.PI;             // one cycle per two beats (left/right)
            float side = Mathf.Sin(half) * (mirror ? -1f : 1f);
            float down = 1f - Mathf.Abs(Mathf.Cos(ph * 0.5f)); // 0 on the beat, dips between
            float hit = Mathf.Pow(1f - Mathf.Repeat(beats, 1f), 4f) + MusicBeatSync.GetPulse(bpm) * pulseBoost * 0.3f;
            float k = intensity * blend;

            Vector3 pos = Vector3.zero; Vector3 rot = Vector3.zero; float legL = 0f, legR = 0f, kneeL = 0f, kneeR = 0f;
            switch (CurrentMove)
            {
                case Move.Groove:
                    pos.y = -down * bounceHeight; rot.z = side * hipSwayDegrees; rot.x = hit * headBobDegrees; kneeL = kneeR = down * legDegrees; break;
                case Move.StepTouch:
                    pos.x = side * stepDistance; pos.y = -down * bounceHeight * 0.7f; rot.z = -side * hipSwayDegrees * 0.6f; rot.x = hit * headBobDegrees * 0.6f;
                    legL = Mathf.Max(0f, side) * legDegrees; legR = Mathf.Max(0f, -side) * legDegrees; kneeL = legL; kneeR = legR; break;
                case Move.Twist:
                    rot.y = side * twistDegrees; pos.y = -down * bounceHeight * 0.8f; kneeL = kneeR = down * legDegrees * 1.2f; rot.x = hit * headBobDegrees * 0.5f; break;
                case Move.Bounce:
                    pos.y = Mathf.Abs(Mathf.Sin(ph * 0.5f)) * bounceHeight * 1.6f; rot.x = hit * headBobDegrees; legL = legR = -down * legDegrees * 0.4f; kneeL = kneeR = down * legDegrees; break;
                case Move.Rock:
                    rot.x = Mathf.Sin(ph * 0.5f) * headBobDegrees * 1.6f; rot.z = side * hipSwayDegrees * 0.5f; pos.y = -down * bounceHeight * 0.5f; kneeL = kneeR = down * legDegrees * 0.6f; break;
                case Move.Shuffle:
                    float q = Mathf.Sin(ph) * (mirror ? -1f : 1f); pos.x = q * stepDistance * 0.6f; rot.y = -q * twistDegrees * 0.5f; pos.y = -down * bounceHeight;
                    legL = q * legDegrees; legR = -q * legDegrees; kneeL = Mathf.Max(0f, q) * legDegrees; kneeR = Mathf.Max(0f, -q) * legDegrees; break;
            }
            pos *= k; rot *= k;
            if (body)
            {
                body.localPosition = bodyBase + pos;
                body.localRotation = bodyRot * Quaternion.Euler(rot);
            }
            Joint(rig.LeftHip, lh, legL * k, -side * 4f * k); Joint(rig.RightHip, rh, legR * k, -side * 4f * k);
            Joint(rig.LeftKnee, lk, -kneeL * k, 0f); Joint(rig.RightKnee, rk, -kneeR * k, 0f);
        }

        void Joint(Transform j, Quaternion rest, float pitch, float roll)
        {
            if (!j) return;
            Vector3 right = j.parent ? j.parent.InverseTransformDirection(transform.right) : transform.right;
            Vector3 fwd = j.parent ? j.parent.InverseTransformDirection(transform.forward) : transform.forward;
            j.localRotation = Quaternion.AngleAxis(roll, fwd) * Quaternion.AngleAxis(pitch, right) * rest;
        }
    }
}

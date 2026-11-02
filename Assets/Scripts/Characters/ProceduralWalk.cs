using UnityEngine;

namespace DungeonUnity
{
    /// Animates CharacterRig legs (hips + optional knees) and the body from actual movement speed.
    /// Works on bones or pivots of any orientation: swing is applied about the character's right axis on top of the rest pose.
    [RequireComponent(typeof(CharacterRig))]
    public class ProceduralWalk : MonoBehaviour
    {
        [Tooltip("Visual body to bob/lean. Defaults to the rig's body (model root).")] public Transform bodyVisual;
        [Tooltip("Overrides rig config values when set.")] public CharacterRigConfig configOverride;
        [Range(0f, 3f), Tooltip("Speed multiplier on the step cycle.")] public float cadence = 1f;
        [Range(0f, 1f), Tooltip("Idle breathing amount.")] public float idleBreath = 0.3f;
        [Tooltip("Flip swing direction if legs swing backwards.")] public bool invertSwing = false;

        CharacterRig rig; CharacterRigConfig cfg; Vector3 lastPos, bodyBase; Quaternion bodyBaseRot;
        Quaternion lh, rh, lk, rk; Vector3 llegPos, rlegPos; int cachedVersion = -1; Transform cachedBody;
        float phase, blend, speed;

        public float Speed => speed;

        void Awake() { rig = GetComponent<CharacterRig>(); lastPos = transform.position; }

        void Cache()
        {
            cachedVersion = rig.Version;
            if (rig.LeftHip) lh = rig.LeftHip.localRotation; if (rig.RightHip) rh = rig.RightHip.localRotation;
            if (rig.LeftKnee) lk = rig.LeftKnee.localRotation; if (rig.RightKnee) rk = rig.RightKnee.localRotation;
            if (rig.LeftHip && rig.LeftHip.childCount > 0) llegPos = rig.LeftHip.GetChild(0).localPosition;
            if (rig.RightHip && rig.RightHip.childCount > 0) rlegPos = rig.RightHip.GetChild(0).localPosition;
            cachedBody = bodyVisual ? bodyVisual : (rig.body ? rig.body : null);
            if (cachedBody == transform) cachedBody = null;
            if (cachedBody) { bodyBase = cachedBody.localPosition; bodyBaseRot = cachedBody.localRotation; }
        }

        void LateUpdate()
        {
            cfg = configOverride ? configOverride : rig.config;
            if (rig.Version != cachedVersion) Cache();
            float stride = cfg ? cfg.strideLength : 0.9f, swingDeg = cfg ? cfg.legSwingDegrees : 30f, kneeDeg = cfg ? cfg.kneeBendDegrees : 35f;
            Vector3 d = transform.position - lastPos; d.y = 0f; lastPos = transform.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            speed = Mathf.Lerp(speed, d.magnitude / dt, dt * 10f);
            blend = Mathf.MoveTowards(blend, speed > 0.1f ? 1f : 0f, dt * (cfg ? cfg.blendSpeed : 8f));
            phase = Mathf.Repeat(phase + (d.magnitude / Mathf.Max(0.05f, stride)) * cadence, 1f);

            float s = Mathf.Sin(phase * Mathf.PI * 2f) * (invertSwing ? -1f : 1f);
            Joint(rig.LeftHip, lh, s * swingDeg * blend);
            Joint(rig.RightHip, rh, -s * swingDeg * blend);
            Joint(rig.LeftKnee, lk, -KneeBend(phase) * kneeDeg * blend * (invertSwing ? -1f : 1f));
            Joint(rig.RightKnee, rk, -KneeBend(Mathf.Repeat(phase + 0.5f, 1f)) * kneeDeg * blend * (invertSwing ? -1f : 1f));
            if (!rig.LeftKnee && cfg) Lift(rig.LeftHip, llegPos, phase);
            if (!rig.RightKnee && cfg) Lift(rig.RightHip, rlegPos, Mathf.Repeat(phase + 0.5f, 1f));

            if (cachedBody && cfg)
            {
                float bob = Mathf.Abs(s) * cfg.bodyBob * blend + Mathf.Sin(Time.time * 1.5f) * 0.01f * idleBreath;
                float lean = Mathf.Clamp01(speed / 4f) * cfg.leanDegrees * blend;
                float sway = s * cfg.swayDegrees * blend;
                cachedBody.localPosition = bodyBase + Vector3.up * bob;
                cachedBody.localRotation = bodyBaseRot * Quaternion.Euler(lean, 0f, sway);
            }
        }

        static float KneeBend(float p) => p < 0.5f ? Mathf.Sin(p * Mathf.PI * 2f) : 0f; // bend during the swing half

        void Joint(Transform j, Quaternion rest, float angle)
        {
            if (!j) return;
            Vector3 axis = j.parent ? j.parent.InverseTransformDirection(transform.right) : transform.right;
            j.localRotation = Quaternion.AngleAxis(angle, axis) * rest;
        }

        void Lift(Transform hip, Vector3 basePos, float legPhase)
        {
            if (!hip || hip.childCount == 0 || !hip.GetChild(0).name.EndsWith("_Leg")) return;
            float lift = legPhase < 0.5f ? cfg.stepCurve.Evaluate(Mathf.Sin(legPhase * Mathf.PI * 2f) * 0.5f + 0.5f) * cfg.legLift * blend : 0f;
            hip.GetChild(0).localPosition = basePos + Vector3.up * lift;
        }
    }
}

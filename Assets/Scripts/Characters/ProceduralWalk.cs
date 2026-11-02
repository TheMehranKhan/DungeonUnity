using UnityEngine;

namespace DungeonUnity
{
    /// Animates CharacterRig legs and the body from actual movement speed: stride phase, leg swing/lift, bob, lean, sway.
    [RequireComponent(typeof(CharacterRig))]
    public class ProceduralWalk : MonoBehaviour
    {
        [Tooltip("Visual body to bob/lean (child). Leave empty to skip body motion.")] public Transform bodyVisual;
        [Tooltip("Overrides rig config values when set.")] public CharacterRigConfig configOverride;
        [Range(0f, 3f), Tooltip("Speed multiplier on the step cycle.")] public float cadence = 1f;
        [Range(0f, 1f), Tooltip("Idle breathing amount.")] public float idleBreath = 0.3f;

        CharacterRig rig; CharacterRigConfig cfg; Vector3 lastPos, bodyBase; Quaternion bodyBaseRot;
        float phase, blend, speed;

        public float Speed => speed;

        void Awake()
        {
            rig = GetComponent<CharacterRig>();
            lastPos = transform.position;
            if (bodyVisual) { bodyBase = bodyVisual.localPosition; bodyBaseRot = bodyVisual.localRotation; }
        }

        void LateUpdate()
        {
            cfg = configOverride ? configOverride : rig.config;
            if (!cfg) return;
            Vector3 d = transform.position - lastPos; d.y = 0f; lastPos = transform.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            speed = Mathf.Lerp(speed, d.magnitude / dt, dt * 10f);
            blend = Mathf.MoveTowards(blend, speed > 0.1f ? 1f : 0f, dt * cfg.blendSpeed);
            phase = Mathf.Repeat(phase + (d.magnitude / Mathf.Max(0.05f, cfg.strideLength)) * cadence, 1f);

            float s = Mathf.Sin(phase * Mathf.PI * 2f);
            float swing = s * cfg.legSwingDegrees * blend;
            Leg(rig.LeftHip, swing, phase);
            Leg(rig.RightHip, -swing, Mathf.Repeat(phase + 0.5f, 1f));

            if (bodyVisual)
            {
                float bob = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f)) * cfg.bodyBob * blend + Mathf.Sin(Time.time * 1.5f) * 0.01f * idleBreath;
                float lean = Mathf.Clamp01(speed / 4f) * cfg.leanDegrees * blend;
                float sway = s * cfg.swayDegrees * blend;
                bodyVisual.localPosition = bodyBase + Vector3.up * bob;
                bodyVisual.localRotation = bodyBaseRot * Quaternion.Euler(lean, 0f, sway);
            }
        }

        void Leg(Transform hip, float angle, float legPhase)
        {
            if (!hip) return;
            hip.localRotation = Quaternion.Euler(angle, 0f, 0f);
            if (hip.childCount > 0)
            {
                var leg = hip.GetChild(0);
                float lift = legPhase < 0.5f ? cfg.stepCurve.Evaluate(Mathf.Sin(legPhase * Mathf.PI * 2f) * 0.5f + 0.5f) * cfg.legLift * blend : 0f;
                leg.localPosition = new Vector3(0f, -cfg.legLength * 0.5f + lift, 0f);
            }
        }
    }
}

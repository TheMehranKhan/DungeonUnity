using UnityEngine;

namespace DungeonUnity
{
    /// Cinematic camera: cuts, blends or orbits between shot points, or follows a target with an offset.
    public class CameraDirector : MonoBehaviour
    {
        public enum ShotMode { Cut, Blend, Orbit, Follow }

        [Header("References")]
        public Camera cam;
        public Transform[] shots = new Transform[0];
        [Tooltip("Point the camera looks at (stage). Optional.")] public Transform lookTarget;
        [Tooltip("Target followed in Follow mode.")] public Transform followTarget;

        [Header("Shots")]
        public ShotMode mode = ShotMode.Blend;
        [Range(1f, 60f)] public float shotSeconds = 6f;
        public bool randomOrder = false;
        [Range(0.1f, 10f)] public float blendSpeed = 1.5f;
        [Tooltip("Easing for position blend (0..1).")] public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Orbit / Follow")]
        [Range(-90f, 90f)] public float orbitSpeed = 5f;
        public Vector3 followOffset = new Vector3(0f, 2.5f, -5f);
        [Range(0.5f, 20f)] public float followSmoothing = 4f;
        public Vector3 lookOffset = new Vector3(0f, 1f, 0f);

        [Header("Lens")]
        [Range(15f, 120f)] public float baseFov = 60f;
        [Range(0f, 30f), Tooltip("FOV punch on each beat.")] public float beatFovKick = 2f;
        [Range(0f, 30f), Tooltip("Slow FOV breathing.")] public float fovBreath = 3f;
        [Range(0.01f, 2f)] public float fovBreathSpeed = 0.1f;

        [Header("Shake")]
        public bool beatShake = true;
        [Range(0f, 0.3f)] public float shakeAmount = 0.03f;
        [Range(60f, 200f)] public float bpm = 128f;

        int index; float timer, blendT = 1f; Vector3 fromPos; Quaternion fromRot;

        void Reset() { cam = GetComponent<Camera>(); }
        void Awake() { if (!cam) cam = GetComponent<Camera>(); }
        void OnValidate() { if (shotSeconds < 1f) shotSeconds = 1f; }

        void LateUpdate()
        {
            if (!cam) return;
            var ct = cam.transform;
            if (mode == ShotMode.Follow && followTarget)
            {
                Vector3 want = followTarget.TransformPoint(followOffset);
                ct.position = Vector3.Lerp(ct.position, want, Time.deltaTime * followSmoothing);
                ct.rotation = Quaternion.Slerp(ct.rotation, Quaternion.LookRotation(followTarget.position + lookOffset - ct.position), Time.deltaTime * followSmoothing);
            }
            else if (shots != null && shots.Length > 0)
            {
                timer += Time.deltaTime;
                if (timer >= shotSeconds) NextShotInternal();
                var s = shots[Mathf.Clamp(index, 0, shots.Length - 1)];
                if (s)
                {
                    if (mode == ShotMode.Orbit && lookTarget) s.RotateAround(lookTarget.position, Vector3.up, orbitSpeed * Time.deltaTime);
                    Quaternion targetRot = lookTarget ? Quaternion.LookRotation(lookTarget.position + lookOffset - s.position) : s.rotation;
                    if (mode == ShotMode.Cut) ct.SetPositionAndRotation(s.position, targetRot);
                    else
                    {
                        blendT = Mathf.Min(1f, blendT + Time.deltaTime * blendSpeed * 0.5f);
                        float k = blendCurve.Evaluate(blendT);
                        ct.position = Vector3.Lerp(fromPos, s.position, k);
                        ct.rotation = Quaternion.Slerp(fromRot, targetRot, k);
                    }
                }
            }
            float pulse = MusicBeatSync.GetPulse(bpm);
            cam.fieldOfView = baseFov + Mathf.Sin(Time.time * fovBreathSpeed * Mathf.PI * 2f) * fovBreath + pulse * beatFovKick;
            if (beatShake) ct.position += Random.insideUnitSphere * shakeAmount * pulse;
        }

        void NextShotInternal()
        {
            timer = 0f; fromPos = cam.transform.position; fromRot = cam.transform.rotation; blendT = 0f;
            index = randomOrder && shots.Length > 1 ? (index + Random.Range(1, shots.Length)) % shots.Length : (index + 1) % shots.Length;
        }

        public void NextShot() { NextShotInternal(); }
    }
}

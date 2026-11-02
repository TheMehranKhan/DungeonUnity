using System.Collections;
using UnityEngine;

namespace DungeonUnity
{
    /// On Play: holds a cinematic shot, flies the camera along an arc to the player's eyes, hands over to the player camera,
    /// enters player mode and starts the AutoPilot self-play tour. Any key skips the flight.
    public class IntroSequence : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Cinematic camera that flies in. Disabled when the intro ends.")] public Camera cinematicCamera;
        public PlayerModeController player;
        [Tooltip("Started in self-play mode after the intro. Optional.")] public AutoPilot autoPilot;
        [Tooltip("Hidden during the intro (welcome overlay etc).")] public GameObject[] hideDuringIntro = new GameObject[0];

        [Header("Timing")]
        public bool playOnStart = true;
        [Range(0f, 10f), Tooltip("Seconds to hold the opening shot.")] public float holdSeconds = 2f;
        [Range(0.5f, 20f), Tooltip("Flight duration.")] public float flySeconds = 4f;
        [Tooltip("Ease of the flight (0..1).")] public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 15f), Tooltip("How high the flight arcs above the straight line.")] public float arcHeight = 3f;
        [Range(0f, 1f), Tooltip("When to start turning toward the player's view (fraction of the flight).")] public float lookBlendStart = 0.4f;

        [Header("Behaviour")]
        [Tooltip("Start the AutoPilot tour immediately when the intro ends.")] public bool startSelfPlay = true;
        [Tooltip("Any key/click skips to the end of the flight.")] public bool allowSkip = true;
        [Tooltip("Stop the CameraDirector shot cycling while flying.")] public bool pauseDirector = true;

        public bool Running { get; private set; }

        void Start() { if (playOnStart) Play(); }

        public void Play() { if (!Running) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            Running = true;
            foreach (var g in hideDuringIntro) if (g) g.SetActive(false);
            if (!cinematicCamera || !player || !player.cameraPivot) { Finish(); yield break; }
            cinematicCamera.gameObject.SetActive(true);
            var dir = cinematicCamera.GetComponent<CameraDirector>();

            float t = 0f;
            while (t < holdSeconds && !Skip(t)) { t += Time.deltaTime; yield return null; }
            if (dir && pauseDirector) dir.enabled = false;

            var cam = cinematicCamera.transform;
            Vector3 p0 = cam.position; Quaternion r0 = cam.rotation;
            t = 0f;
            while (t < flySeconds)
            {
                if (Skip(t + holdSeconds)) break;
                t += Time.deltaTime;
                float k = flyCurve.Evaluate(Mathf.Clamp01(t / flySeconds));
                Vector3 p1 = player.cameraPivot.position;
                Vector3 mid = (p0 + p1) * 0.5f + Vector3.up * arcHeight;
                cam.position = Vector3.Lerp(Vector3.Lerp(p0, mid, k), Vector3.Lerp(mid, p1, k), k);
                Quaternion lookAlong = Quaternion.LookRotation((player.transform.position + Vector3.up - cam.position).sqrMagnitude > 0.01f ? player.transform.position + Vector3.up - cam.position : cam.forward);
                float lk = Mathf.InverseLerp(lookBlendStart, 1f, k);
                cam.rotation = Quaternion.Slerp(Quaternion.Slerp(r0, lookAlong, Mathf.Clamp01(k / Mathf.Max(0.01f, lookBlendStart))), player.cameraPivot.rotation, lk);
                yield return null;
            }
            Finish();
        }

        bool Skip(float elapsed) => allowSkip && elapsed > 0.3f && Input.anyKeyDown;

        void Finish()
        {
            if (cinematicCamera) cinematicCamera.gameObject.SetActive(false);
            if (player) player.SetPlayerMode(true);
            if (startSelfPlay && autoPilot) autoPilot.BeginSelfPlay();
            Running = false;
        }
    }
}

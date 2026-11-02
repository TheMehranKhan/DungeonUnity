using UnityEngine;

namespace DungeonUnity
{
    /// Plays footstep clips based on distance walked by a CharacterController.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        public AudioClip[] steps = new AudioClip[0];
        [Tooltip("Plays footsteps from here; created if empty.")] public AudioSource source;
        [Range(0.3f, 3f), Tooltip("Metres between steps when walking.")] public float stepDistance = 1.6f;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Range(0f, 0.5f)] public float pitchJitter = 0.08f;
        [Range(0f, 1f), Tooltip("Below this speed no steps play.")] public float minSpeed = 0.2f;

        CharacterController cc; Vector3 last; float travelled; int lastIndex = -1;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (!source) { source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0f; }
            last = transform.position;
        }

        void Update()
        {
            Vector3 p = transform.position; Vector3 d = p - last; d.y = 0f; last = p;
            if (!cc.isGrounded || steps == null || steps.Length == 0) return;
            float speed = d.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
            if (speed < minSpeed) return;
            travelled += d.magnitude;
            if (travelled < stepDistance) return;
            travelled = 0f;
            int i = Random.Range(0, steps.Length);
            if (steps.Length > 1 && i == lastIndex) i = (i + 1) % steps.Length;
            lastIndex = i;
            if (!steps[i]) return;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(steps[i], volume * GameSettings.Sfx);
        }
    }
}

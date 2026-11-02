using UnityEngine;

namespace DungeonUnity
{
    /// A glowing pill: bobs and spins, and when taken triggers the TrippyEffect. Optional respawn.
    public class PillPickup : Interactable
    {
        [Header("Trip")]
        [Range(1f, 60f)] public float tripSeconds = 15f;
        [Range(0f, 1f)] public float tripIntensity = 1f;

        [Header("Idle motion")]
        [Range(0f, 360f)] public float spinSpeed = 90f;
        [Range(0f, 0.5f)] public float bobHeight = 0.05f;
        [Range(0.1f, 5f)] public float bobSpeed = 1.5f;

        [Header("Respawn")]
        public bool respawn = true;
        [Range(1f, 300f)] public float respawnSeconds = 30f;

        Vector3 basePos; float respawnAt = -1f; Renderer[] rends; Collider col;

        void Reset() { prompt = "Press {key} to take the pill"; sfx = "PillPickup"; }

        void Awake() { basePos = transform.localPosition; rends = GetComponentsInChildren<Renderer>(); col = GetComponent<Collider>(); }

        public override bool CanInteract => base.CanInteract && respawnAt < 0f;

        protected override void OnInteract(GameObject user)
        {
            TrippyEffect.TriggerGlobal(tripSeconds, tripIntensity);
            SetVisible(false);
            respawnAt = respawn ? Time.time + respawnSeconds : float.MaxValue;
        }

        void Update()
        {
            if (respawnAt >= 0f && Time.time >= respawnAt) { respawnAt = -1f; SetVisible(true); }
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobHeight;
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void SetVisible(bool v)
        {
            if (rends != null) foreach (var r in rends) if (r) r.enabled = v;
            if (col) col.enabled = v;
        }
    }
}

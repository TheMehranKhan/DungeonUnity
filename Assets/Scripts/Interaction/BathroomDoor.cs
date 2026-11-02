using UnityEngine;

namespace DungeonUnity
{
    /// Hinged door that swings open/closed with a creak.
    public class BathroomDoor : Interactable
    {
        [Tooltip("Pivot rotated around Y. Defaults to this transform.")] public Transform hinge;
        [Range(-170f, 170f)] public float openAngle = 95f;
        [Range(0.5f, 20f)] public float speed = 4f;
        public AnimationCurve swing = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool startOpen = false;
        [Range(0f, 60f), Tooltip("Auto close after seconds (0 = never).")] public float autoClose = 0f;

        Quaternion closedRot; float t, target, openedAt;
        public bool IsOpen => target > 0.5f;

        void Reset() { prompt = "Press {key} to open/close"; sfx = "DoorCreak"; }

        void Awake()
        {
            if (!hinge) hinge = transform;
            closedRot = hinge.localRotation;
            t = target = startOpen ? 1f : 0f; Apply();
        }

        protected override void OnInteract(GameObject user) { target = IsOpen ? 0f : 1f; openedAt = Time.time; }

        void Update()
        {
            if (autoClose > 0f && IsOpen && Time.time - openedAt > autoClose) { target = 0f; AudioManager.Play(sfx, transform.position, 0.7f); }
            if (Mathf.Approximately(t, target)) return;
            t = Mathf.MoveTowards(t, target, Time.deltaTime * speed * 0.5f);
            Apply();
        }

        void Apply() { if (hinge) hinge.localRotation = closedRot * Quaternion.Euler(0f, openAngle * swing.Evaluate(t), 0f); }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Raycasts from the camera, shows the prompt of the Interactable in view and triggers it on its key.
    public class InteractionPrompt : MonoBehaviour
    {
        public Camera cam;
        [Tooltip("UI text for the prompt.")] public Text promptText;
        [Tooltip("Optional crosshair that lights up on targets.")] public Graphic crosshair;
        public Color crosshairIdle = new Color(1f, 1f, 1f, 0.4f);
        public Color crosshairActive = new Color(1f, 0.2f, 0.4f, 1f);
        [Range(0.5f, 20f)] public float rayDistance = 5f;
        public LayerMask mask = ~0;
        [Tooltip("Only active while this player is in player mode.")] public PlayerModeController player;

        public Interactable Current { get; private set; }

        void Awake() { if (!cam) cam = Camera.main; if (promptText) promptText.text = ""; }

        void Update()
        {
            Current = null;
            bool active = cam && (!player || (player.Active && !player.Paused));
            if (active && Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, rayDistance, mask, QueryTriggerInteraction.Collide))
            {
                var it = hit.collider.GetComponentInParent<Interactable>();
                if (it && it.CanInteract && hit.distance <= it.maxDistance) Current = it;
            }
            if (promptText) promptText.text = Current ? Current.PromptText : "";
            if (crosshair) { crosshair.enabled = active; crosshair.color = Current ? crosshairActive : crosshairIdle; }
            if (Current && InputCompat.InteractDown(Current.key)) Current.Interact(player ? player.gameObject : gameObject);
        }
    }
}

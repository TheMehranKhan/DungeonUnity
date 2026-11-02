using UnityEngine;
using UnityEngine.Events;

namespace DungeonUnity
{
    /// Base for anything the player can look at and press a key on. Needs a collider for the raycast.
    [RequireComponent(typeof(Collider))]
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Interaction")]
        [Tooltip("Shown on screen, {key} is replaced by the key name.")] public string prompt = "Press {key} to interact";
        public KeyCode key = KeyCode.E;
        [Range(0.5f, 10f)] public float maxDistance = 3f;
        [Tooltip("Named AudioManager SFX played on use.")] public string sfx = "";
        [Range(0f, 10f), Tooltip("Seconds before it can be used again.")] public float cooldown = 0.3f;
        public UnityEvent onInteract = new UnityEvent();

        float nextUse;
        public virtual bool CanInteract => isActiveAndEnabled && Time.time >= nextUse;
        public virtual string PromptText => (prompt ?? "").Replace("{key}", key.ToString());

        public void Interact(GameObject user)
        {
            if (!CanInteract) return;
            nextUse = Time.time + cooldown;
            if (!string.IsNullOrEmpty(sfx)) AudioManager.Play(sfx, transform.position);
            OnInteract(user);
            onInteract?.Invoke();
        }

        protected abstract void OnInteract(GameObject user);
    }
}

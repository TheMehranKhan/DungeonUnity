using UnityEngine;

namespace DungeonUnity
{
    /// Plays its sfx and fires onInteract. Handy for props like the toilet flush.
    public class SoundInteractable : Interactable
    {
        protected override void OnInteract(GameObject user) { }
    }
}

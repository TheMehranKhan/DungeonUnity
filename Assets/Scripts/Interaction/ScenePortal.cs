using UnityEngine;

namespace DungeonUnity
{
    /// Interact (or walk in, if trigger mode) to fade to another scene.
    public class ScenePortal : Interactable
    {
        public string sceneName = "Welcome";
        [Tooltip("Also load when the player walks into a trigger collider.")] public bool loadOnTriggerEnter = false;

        void Reset() { prompt = "Press {key} to leave"; }

        protected override void OnInteract(GameObject user) { SceneFader.LoadScene(sceneName); }

        void OnTriggerEnter(Collider other)
        {
            if (loadOnTriggerEnter && other.GetComponent<PlayerModeController>()) SceneFader.LoadScene(sceneName);
        }
    }
}

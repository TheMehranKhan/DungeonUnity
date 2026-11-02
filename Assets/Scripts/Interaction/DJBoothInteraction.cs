using UnityEngine;

namespace DungeonUnity
{
    /// DJ booth: toggles music, lasers and rave lights together.
    public class DJBoothInteraction : Interactable
    {
        [Tooltip("Root object of all laser emitters.")] public GameObject lasers;
        public DungeonRaveLighting raveLighting;
        public bool toggleMusic = true;
        public bool toggleLasers = true;
        public bool toggleLighting = false;
        [Tooltip("Plays this sfx when the music comes back on.")] public string dropSfx = "BassDrop";

        void Reset() { prompt = "Press {key} to {state} the music"; }

        public override string PromptText
        {
            get
            {
                bool on = AudioManager.Instance && AudioManager.Instance.MusicOn;
                return base.PromptText.Replace("{state}", on ? "stop" : "start");
            }
        }

        protected override void OnInteract(GameObject user)
        {
            bool on = true;
            if (toggleMusic && AudioManager.Instance) { AudioManager.Instance.ToggleMusic(); on = AudioManager.Instance.MusicOn; }
            if (toggleLasers && lasers) lasers.SetActive(on);
            if (toggleLighting && raveLighting) raveLighting.enabled = on;
            if (on && !string.IsNullOrEmpty(dropSfx)) AudioManager.Play(dropSfx, transform.position);
        }
    }
}

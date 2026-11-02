using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Esc / P pauses: freezes time, frees the cursor, shows Resume / Settings / Quit.
    public class PauseMenu : MonoBehaviour
    {
        public GameObject panel;
        public SettingsMenu settings;
        public PlayerModeController player;
        public Button resumeButton, settingsButton, quitButton;
        public string quitScene = "Welcome";
        [Tooltip("Pause the music too.")] public bool pauseAudio = true;

        public bool IsPaused { get; private set; }

        void Start()
        {
            if (panel) panel.SetActive(false);
            if (resumeButton) resumeButton.onClick.AddListener(Resume);
            if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton) quitButton.onClick.AddListener(QuitToMenu);
        }

        void Update()
        {
            if (!InputCompat.PauseDown) return;
            if (settings && settings.IsOpen) { settings.Close(); return; }
            if (IsPaused) Resume();
            else if (!player || player.Active) Pause();
        }

        public void Pause()
        {
            IsPaused = true; Time.timeScale = 0f;
            if (pauseAudio) AudioListener.pause = true;
            if (panel) panel.SetActive(true);
            if (player) player.Paused = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void Resume()
        {
            IsPaused = false; Time.timeScale = 1f; AudioListener.pause = false;
            if (panel) panel.SetActive(false);
            if (settings) settings.Close();
            if (player) { player.Paused = false; if (player.Active) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } }
        }

        public void OpenSettings() { if (settings) settings.Open(); }

        public void QuitToMenu() { Time.timeScale = 1f; AudioListener.pause = false; SceneFader.LoadScene(quitScene); }

        void OnDisable() { if (IsPaused) { Time.timeScale = 1f; AudioListener.pause = false; } }
    }
}

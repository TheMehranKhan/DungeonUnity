using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Title panel with a Player Mode button; fades out and hands control to the player.
    public class WelcomeScreen : MonoBehaviour
    {
        public GameObject welcomePanel;
        public Button playButton;
        public PlayerModeController playerMode;
        public CanvasGroup fadeGroup;
        [Tooltip("Disabled when entering player mode (e.g. the cinematic camera).")] public GameObject[] disableOnPlay = new GameObject[0];
        [Range(0.05f, 5f)] public float fadeSeconds = 0.6f;
        [Tooltip("Press any key/Enter to start too.")] public bool startOnEnter = true;
        [Tooltip("Animated title (optional).")] public Text title;
        public Gradient titleColors = DungeonConfig.DefaultGradient();
        [Range(0f, 2f)] public float titleCycleSpeed = 0.3f;

        float fadeT = -1f;

        void Start()
        {
            if (welcomePanel) welcomePanel.SetActive(true);
            if (fadeGroup) fadeGroup.alpha = 1f;
            if (playerMode) playerMode.SetPlayerMode(false);
            if (playButton) playButton.onClick.AddListener(EnterPlayerMode);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void OnDestroy() { if (playButton) playButton.onClick.RemoveListener(EnterPlayerMode); }

        public void EnterPlayerMode() { if (fadeT < 0f) fadeT = 0f; }

        void Update()
        {
            if (title && titleColors != null) title.color = titleColors.Evaluate(Mathf.Repeat(Time.unscaledTime * titleCycleSpeed, 1f));
            if (startOnEnter && fadeT < 0f && welcomePanel && welcomePanel.activeSelf && playerMode && !playerMode.Active && InputCompat.Down(KeyCode.Return)) EnterPlayerMode();
            if (fadeT < 0f) return;
            fadeT += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(fadeT / fadeSeconds);
            if (fadeGroup) fadeGroup.alpha = a;
            if (a > 0f) return;
            fadeT = -1f;
            if (welcomePanel) welcomePanel.SetActive(false);
            if (fadeGroup) fadeGroup.alpha = 1f;
            foreach (var g in disableOnPlay) if (g) g.SetActive(false);
            if (playerMode) playerMode.SetPlayerMode(true);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

/// Welcome screen: shows a title panel with a "Enter Player Mode" button.
public class WelcomeScreen : MonoBehaviour
{
    public GameObject welcomePanel;
    public Button playButton;
    public PlayerModeController playerMode;
    public CanvasGroup fadeGroup;
    public float fadeSeconds = 0.6f;
    float fadeT = -1f;

    void Start()
    {
        if (welcomePanel) welcomePanel.SetActive(true);
        if (playerMode) playerMode.SetPlayerMode(false);
        if (playButton) playButton.onClick.AddListener(EnterPlayerMode);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void EnterPlayerMode() { fadeT = 0f; }

    void Update()
    {
        if (fadeT < 0f) return;
        fadeT += Time.unscaledDeltaTime;
        float a = 1f - Mathf.Clamp01(fadeT / fadeSeconds);
        if (fadeGroup) fadeGroup.alpha = a;
        if (a <= 0f)
        {
            fadeT = -1f;
            if (welcomePanel) welcomePanel.SetActive(false);
            if (playerMode) playerMode.SetPlayerMode(true);
        }
    }
}

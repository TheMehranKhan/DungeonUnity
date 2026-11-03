using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Volume, sensitivity and invert-Y, saved in PlayerPrefs through GameSettings.
    public class SettingsMenu : MonoBehaviour
    {
        public GameObject panel;
        public Slider master, music, sfx, sensitivity;
        public Toggle invertY;
        public Button backButton;

        public bool IsOpen => panel && panel.activeSelf;

        void Start()
        {
            Refresh();
            Hook(master, v => GameSettings.Master = v);
            Hook(music, v => GameSettings.Music = v);
            Hook(sfx, v => GameSettings.Sfx = v);
            if (sensitivity) { sensitivity.minValue = 0.1f; sensitivity.maxValue = 10f; sensitivity.SetValueWithoutNotify(GameSettings.Sensitivity); }
            Hook(sensitivity, v => GameSettings.Sensitivity = v);
            if (invertY) invertY.onValueChanged.AddListener(v => GameSettings.InvertY = v);
            if (backButton) backButton.onClick.AddListener(Close);
            if (panel) panel.SetActive(false);
        }

        static void Hook(Slider s, UnityEngine.Events.UnityAction<float> a) { if (s) s.onValueChanged.AddListener(a); }

        public void Refresh()
        {
            if (master) master.SetValueWithoutNotify(GameSettings.Master);
            if (music) music.SetValueWithoutNotify(GameSettings.Music);
            if (sfx) sfx.SetValueWithoutNotify(GameSettings.Sfx);
            if (sensitivity) sensitivity.SetValueWithoutNotify(GameSettings.Sensitivity);
            if (invertY) invertY.SetIsOnWithoutNotify(GameSettings.InvertY);
        }

        public void Open() { Refresh(); if (panel) panel.SetActive(true); }
        public void Close() { if (panel) panel.SetActive(false); PlayerPrefs.Save(); }
    }
}

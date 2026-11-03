using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Music menu (M): a button per track plus Auto, Previous, Next. N skips without opening the menu.
    /// Track buttons are cloned from trackButtonTemplate into trackList.
    public class MusicMenu : MonoBehaviour
    {
        public MusicPlayer player;
        public GameObject panel;
        public Text nowPlaying;
        public RectTransform trackList;
        public Button trackButtonTemplate, prevButton, nextButton, autoButton, closeButton;
        public PlayerModeController playerMode;
        public KeyCode toggleKey = KeyCode.M;
        public Color selectedColor = new Color(0.1f, 1f, 0.8f), normalColor = new Color(1f, 0.1f, 0.4f);

        readonly List<Button> buttons = new List<Button>();
        public bool IsOpen => panel && panel.activeSelf;

        void Start()
        {
            if (!player) player = MusicPlayer.Instance ? MusicPlayer.Instance : FindObjectOfType<MusicPlayer>();
            if (panel) panel.SetActive(false);
            if (trackButtonTemplate) trackButtonTemplate.gameObject.SetActive(false);
            if (prevButton) prevButton.onClick.AddListener(() => player.Previous());
            if (nextButton) nextButton.onClick.AddListener(() => player.Next());
            if (autoButton) autoButton.onClick.AddListener(() => player.Auto());
            if (closeButton) closeButton.onClick.AddListener(Close);
            Build();
            MusicPlayer.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { MusicPlayer.Changed -= Refresh; }

        void Build()
        {
            if (!player || !trackButtonTemplate || !trackList) return;
            for (int i = 0; i < player.tracks.Length; i++)
            {
                int idx = i;
                var b = Instantiate(trackButtonTemplate, trackList);
                b.gameObject.SetActive(true); b.name = "Track_" + player.tracks[i].name;
                var rt = b.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(0, -i * (rt.sizeDelta.y + 12f));
                var t = b.GetComponentInChildren<Text>(); if (t) t.text = player.tracks[i].name;
                b.onClick.AddListener(() => player.Select(idx));
                buttons.Add(b);
            }
        }

        void Refresh()
        {
            if (nowPlaying && player) nowPlaying.text = "NOW: " + player.CurrentName + (player.Index < 0 && RoomZone.Current ? "  [" + RoomZone.Current.Title + "]" : "");
            for (int i = 0; i < buttons.Count; i++) if (buttons[i]) buttons[i].GetComponent<Image>().color = player && player.Index == i ? selectedColor : normalColor;
            if (autoButton) autoButton.GetComponent<Image>().color = player && player.Index < 0 ? selectedColor : normalColor;
        }

        void Update()
        {
            if (InputCompat.Down(toggleKey)) { if (IsOpen) Close(); else Open(); }
            if (IsOpen && Time.frameCount % 15 == 0) Refresh();
        }

        public void Open()
        {
            if (!panel) return;
            panel.SetActive(true); Refresh();
            if (playerMode) playerMode.Paused = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void Close()
        {
            if (!panel) return;
            panel.SetActive(false);
            if (playerMode) { playerMode.Paused = false; if (playerMode.Active) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } }
        }
    }
}

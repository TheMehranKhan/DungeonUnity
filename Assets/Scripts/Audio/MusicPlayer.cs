using System;
using UnityEngine;

namespace DungeonUnity
{
    /// The track list behind the music menu. "Auto" lets each RoomZone pick its own music.
    /// Picking a track overrides the rooms until you choose Auto again.
    /// Per-room music is set on each RoomZone (music clip, or trackName from this list).
    public class MusicPlayer : MonoBehaviour
    {
        [Serializable] public class Track { public string name = "Track"; public AudioClip clip; }

        public static MusicPlayer Instance { get; private set; }
        /// True while a track picked in the menu overrides room music.
        public static bool ManualOverride => Instance && Instance.Index >= 0;
        public static event Action Changed;

        public Track[] tracks = new Track[0];
        [Range(0f, 10f)] public float crossfadeSeconds = 1.5f;
        public KeyCode nextKey = KeyCode.N;

        /// -1 = Auto (room music).
        public int Index { get; private set; } = -1;
        public string CurrentName => Index < 0 ? "Auto (room)" : tracks[Index].name;

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        void Update() { if (nextKey != KeyCode.None && InputCompat.Down(nextKey)) Next(); }

        public AudioClip Find(string trackName)
        {
            if (string.IsNullOrEmpty(trackName)) return null;
            foreach (var t in tracks) if (t != null && t.name == trackName) return t.clip;
            return null;
        }

        public void Select(int i)
        {
            if (tracks.Length == 0) return;
            Index = Mathf.Clamp(i, -1, tracks.Length - 1);
            var am = AudioManager.Instance;
            if (am)
            {
                if (Index >= 0) am.CrossfadeMusic(tracks[Index].clip, crossfadeSeconds);
                else if (RoomZone.Current) am.CrossfadeMusic(RoomZone.Current.ResolveMusic(), crossfadeSeconds);
            }
            AudioManager.Play("UIClick");
            Changed?.Invoke();
        }

        public void Next() => Select(Index + 1 >= tracks.Length ? -1 : Index + 1);
        public void Previous() => Select(Index - 1 < -1 ? tracks.Length - 1 : Index - 1);
        public void Auto() => Select(-1);
    }
}

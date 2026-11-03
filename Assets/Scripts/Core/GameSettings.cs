using System;
using UnityEngine;

namespace DungeonUnity
{
    /// Player settings persisted in PlayerPrefs.
    public static class GameSettings
    {
        const string KMaster = "du.master", KMusic = "du.music", KSfx = "du.sfx", KSens = "du.sens", KInvert = "du.invert";
        public static event Action Changed;

        public static float Master { get => PlayerPrefs.GetFloat(KMaster, 0.9f); set => Set(KMaster, value); }
        public static float Music { get => PlayerPrefs.GetFloat(KMusic, 0.8f); set => Set(KMusic, value); }
        public static float Sfx { get => PlayerPrefs.GetFloat(KSfx, 0.9f); set => Set(KSfx, value); }
        public static float Sensitivity { get => PlayerPrefs.GetFloat(KSens, 2f); set => Set(KSens, Mathf.Clamp(value, 0.1f, 10f)); }
        public static bool InvertY { get => PlayerPrefs.GetInt(KInvert, 0) == 1; set { PlayerPrefs.SetInt(KInvert, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); } }

        static void Set(string key, float v) { PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}

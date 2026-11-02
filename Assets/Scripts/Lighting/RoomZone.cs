using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonUnity
{
    /// Trigger volume for one room. When the player enters it crossfades the music, the ambience and the
    /// lighting (palette from DungeonConfig, local room lights, lasers) to this room. Zones stack, so
    /// leaving a corridor that overlaps a room falls back to the room you are still standing in.
    [RequireComponent(typeof(BoxCollider))]
    public class RoomZone : MonoBehaviour
    {
        static readonly List<RoomZone> occupied = new List<RoomZone>();
        static readonly List<RoomZone> all = new List<RoomZone>();
        public static RoomZone Current { get; private set; }
        /// Raised with the new room whenever the current room changes.
        public static event Action<RoomZone> Changed;

        [Header("Room")]
        [Tooltip("Palette name in DungeonConfig.rooms.")] public string roomName = "Main Rave Hall";
        [Tooltip("Shown by RoomTitle when you walk in. Empty = roomName.")] public string displayName = "";
        [Tooltip("Applied at start, and when the player is in no zone.")] public bool isDefault = false;
        [Tooltip("Higher wins when zones overlap (corridor inside a room, etc.).")] public int priority = 0;
        public string playerTag = "Player";

        [Header("Audio")]
        public bool changeMusic = true;
        [Tooltip("Music loop for this room. Empty with changeMusic on = silence.")] public AudioClip music;
        public bool changeAmbience = true;
        public AudioClip ambience;
        [Range(-1f, 10f), Tooltip("Crossfade seconds (-1 = AudioManager default).")] public float crossfadeSeconds = -1f;

        [Header("Lighting")]
        public DungeonConfig config;
        [Tooltip("Global rave lighting that blends ambient, fog and palette.")] public DungeonRaveLighting lighting;
        [Tooltip("Lights that belong to this room. They fade up when you enter and down when you leave.")] public Light[] roomLights = new Light[0];
        [Range(0f, 1f), Tooltip("Room light intensity while you are elsewhere (fraction of their normal value).")] public float idleLightFraction = 0.35f;
        [Range(0.05f, 10f)] public float lightFadeSeconds = 1.5f;
        [Tooltip("Shape of the light fade.")] public AnimationCurve lightFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Laser rigs recoloured and toggled by the palette's lasersOn.")] public LaserShowController[] lasers = new LaserShowController[0];
        [Tooltip("Optional object (lasers root, smoke...) enabled only while in this room.")] public GameObject[] onlyWhileInside = new GameObject[0];

        float[] baseIntensity = new float[0]; float weight;

        public string Title => string.IsNullOrEmpty(displayName) ? roomName : displayName;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void Awake()
        {
            var bc = GetComponent<BoxCollider>(); if (bc) bc.isTrigger = true;
            baseIntensity = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++) baseIntensity[i] = roomLights[i] ? roomLights[i].intensity : 0f;
        }

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); occupied.Remove(this); if (Current == this) Current = null; }

        void Start()
        {
            weight = isDefault ? 1f : 0f; ApplyLights();
            if (isDefault && !Current) Enter(this, true);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (!occupied.Contains(this)) occupied.Add(this);
            Refresh();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            occupied.Remove(this);
            Refresh();
        }

        static void Refresh()
        {
            RoomZone best = null;
            foreach (var z in occupied) if (z && (!best || z.priority >= best.priority)) best = z;
            if (!best) foreach (var z in all) if (z && z.isDefault) { best = z; break; }
            if (best && best != Current) Enter(best, false);
        }

        static void Enter(RoomZone z, bool instant)
        {
            Current = z;
            var am = AudioManager.Instance;
            if (am)
            {
                float s = instant ? 0f : z.crossfadeSeconds;
                if (z.changeMusic) am.CrossfadeMusic(z.music, s);
                if (z.changeAmbience) am.CrossfadeAmbience(z.ambience, s);
            }
            var r = z.config ? z.config.GetRoom(z.roomName) : null;
            if (r != null)
            {
                if (z.lighting) z.lighting.ApplyRoom(r, instant);
                foreach (var l in z.lasers) if (l) { l.SetColor(r.laserColor); l.gameObject.SetActive(r.lasersOn); }
            }
            foreach (var other in all)
                foreach (var g in other.onlyWhileInside) if (g) g.SetActive(other == z);
            Changed?.Invoke(z);
        }

        void Update()
        {
            float target = Current == this ? 1f : 0f;
            if (Mathf.Approximately(weight, target)) return;
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime / lightFadeSeconds);
            ApplyLights();
        }

        void ApplyLights()
        {
            float k = Mathf.Lerp(idleLightFraction, 1f, lightFadeCurve.Evaluate(weight));
            for (int i = 0; i < roomLights.Length && i < baseIntensity.Length; i++) if (roomLights[i]) roomLights[i].intensity = baseIntensity[i] * k;
        }

        void OnDrawGizmos()
        {
            var bc = GetComponent<BoxCollider>(); if (!bc) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.2f, 0.8f, Current == this ? 0.25f : 0.08f);
            Gizmos.DrawCube(bc.center, bc.size);
        }
    }
}

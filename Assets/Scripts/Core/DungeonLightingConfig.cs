using System;
using UnityEngine;

namespace DungeonUnity
{
    /// Per-room lighting look used by RoomLightingRig. Matched by RoomZone.roomName.
    [Serializable]
    public class RoomLightLook
    {
        [Tooltip("Room name, matched by RoomZone.roomName.")] public string name = "Main Rave Hall";
        [Header("Key light (main, casts shadows)")]
        public Color keyColor = new Color(1f, 0.85f, 1f);
        [Range(0f, 30f)] public float keyIntensity = 6f;
        [Range(10f, 160f)] public float keyAngle = 95f;
        [Header("Fill light (soft, no shadows)")]
        public Color fillColor = new Color(0.45f, 0.35f, 1f);
        [Range(0f, 15f)] public float fillIntensity = 2.5f;
        [Header("Rim light (back edge)")]
        public Color rimColor = new Color(0.2f, 1f, 1f);
        [Range(0f, 20f)] public float rimIntensity = 4f;
        [Header("Neon strips")]
        public bool neonStrips = true;
        [ColorUsage(false, true)] public Color neonColor = new Color(1f, 0.1f, 0.9f) * 3f;
        [Tooltip("Second strip colour (ceiling).")][ColorUsage(false, true)] public Color neonColor2 = new Color(0.1f, 0.9f, 1f) * 3f;
        [Range(0f, 1f), Tooltip("Small point lights along the strips so they really light the walls.")] public float stripLightAmount = 0.6f;
    }

    /// Lighting preset for DungeonLightingUpgrade. Create via Assets > Create > DungeonUnity > Lighting Config.
    [CreateAssetMenu(menuName = "DungeonUnity/Lighting Config", fileName = "DungeonLightingConfig")]
    public class DungeonLightingConfig : ScriptableObject
    {
        [Header("Global")]
        [Range(0f, 10f), Tooltip("Multiplies every room palette's ambient colour.")] public float ambientBoost = 3.5f;
        public bool trilightAmbient = true;
        [Range(0f, 8f), Tooltip("Floor for the rave lights so rooms never go fully black.")] public float raveMinIntensity = 1.2f;
        [Range(0f, 2f), Tooltip("Scales every light the upgrade creates.")] public float globalLightScale = 1f;

        [Header("Base Ambient")]
        [Tooltip("Adds a dim ambient floor so rooms are dark but readable.")] public bool baseAmbient = true;
        public Color baseSkyColor = new Color(0.22f, 0.16f, 0.38f);
        public Color baseEquatorColor = new Color(0.12f, 0.1f, 0.24f);
        public Color baseGroundColor = new Color(0.05f, 0.04f, 0.08f);
        [Range(0f, 1f)] public float baseAmbientIntensity = 0.3f;

        [Header("Room Fill")]
        [Tooltip("Soft shadowless point light near each room's ceiling centre.")] public bool roomFill = true;
        public Color roomFillColor = new Color(0.55f, 0.5f, 0.85f);
        [Range(0f, 5f)] public float roomFillIntensity = 0.8f;
        [Range(0.3f, 2f), Tooltip("Range as a multiple of the room diagonal.")] public float roomFillRange = 0.8f;

        [Header("Character Light")]
        [Tooltip("Small shadowless light that follows every NPC and the player.")] public bool characterLight = true;
        public Color characterLightColor = new Color(1f, 0.85f, 0.95f);
        [Range(0f, 5f)] public float characterLightIntensity = 0.9f;
        [Range(0.5f, 8f)] public float characterLightRange = 3f;
        [Tooltip("Offset from the character root (up, back toward camera).")] public Vector3 characterLightOffset = new Vector3(0.4f, 2.2f, -0.8f);
        [Tooltip("Swap black/unlit/broken character materials to URP Lit at runtime.")] public bool fixCharacterMaterials = true;

        [Header("Shadows")]
        public LightShadows keyShadows = LightShadows.Soft;
        [Range(0f, 1f)] public float shadowStrength = 0.75f;
        [Tooltip("Rave/room lights from Build Scene also cast shadows (costly).")] public bool raveLightShadows = false;

        [Header("Probes")]
        public bool reflectionProbes = true;
        [Range(16, 512)] public int reflectionResolution = 128;
        public bool lightProbes = true;
        [Range(1f, 10f), Tooltip("Spacing in metres of the light probe grid.")] public float lightProbeSpacing = 4f;

        [Header("Post FX (URP Volume)")]
        [Range(0f, 10f)] public float bloomIntensity = 2.2f;
        [Range(0f, 2f)] public float bloomThreshold = 0.9f;
        [Range(0f, 1f)] public float bloomScatter = 0.7f;
        [Tooltip("None, Neutral or ACES.")] public string tonemapping = "ACES";
        [Range(-1f, 1.5f), Tooltip("Keep 0 to +0.5 so blacks are not crushed.")] public float postExposure = 0.3f;
        [Range(-100f, 100f)] public float saturation = 15f;
        [Range(-100f, 100f)] public float contrast = 5f;
        [Range(0f, 1f)] public float vignette = 0.25f;

        [Header("Doors")]
        public bool doorFrames = true;
        [ColorUsage(false, true)] public Color doorTrimClosed = new Color(1f, 0.15f, 0.6f) * 2.5f;
        [ColorUsage(false, true)] public Color doorTrimOpen = new Color(0.2f, 1f, 0.5f) * 2.5f;
        [Range(0f, 10f)] public float doorLightIntensity = 2.5f;
        [Tooltip("Doors open by themselves when someone walks into the doorway.")] public bool doorwayTriggers = true;
        [Range(0.1f, 3f), Tooltip("Seconds for a door to swing fully open.")] public float doorOpenSeconds = 0.8f;
        [Range(0f, 30f), Tooltip("Auto close seconds after the doorway is empty (0 = never).")] public float doorAutoClose = 4f;

        [Header("Rooms")]
        public RoomLightLook[] rooms = DefaultRooms();

        public static RoomLightLook[] DefaultRooms() => new[]
        {
            new RoomLightLook { name = "Main Rave Hall" },
            new RoomLightLook { name = "Red Light Corridor", keyColor = new Color(1f, 0.25f, 0.2f), keyIntensity = 4f, fillColor = new Color(0.6f, 0.05f, 0.1f), rimColor = new Color(1f, 0.4f, 0.1f), neonColor = new Color(1f, 0.05f, 0.05f) * 3f, neonColor2 = new Color(1f, 0.2f, 0.1f) * 2f },
            new RoomLightLook { name = "Bathroom", keyColor = new Color(0.85f, 1f, 0.9f), keyIntensity = 5f, fillColor = new Color(0.5f, 0.9f, 0.7f), rimColor = new Color(0.3f, 1f, 0.6f), neonColor = new Color(0.2f, 1f, 0.5f) * 2.5f, neonColor2 = new Color(0.6f, 1f, 0.9f) * 2f },
            new RoomLightLook { name = "Trippy Chill Room", keyColor = new Color(0.7f, 0.5f, 1f), keyIntensity = 4.5f, fillColor = new Color(0.2f, 0.8f, 1f), rimColor = new Color(1f, 0.3f, 1f), neonColor = new Color(0.5f, 0.2f, 1f) * 3f, neonColor2 = new Color(0.1f, 1f, 0.8f) * 3f },
            new RoomLightLook { name = "VIP Backstage", keyColor = new Color(1f, 0.85f, 0.55f), keyIntensity = 6f, fillColor = new Color(1f, 0.4f, 0.6f), rimColor = new Color(1f, 0.75f, 0.2f), neonColor = new Color(1f, 0.7f, 0.1f) * 3f, neonColor2 = new Color(1f, 0.2f, 0.5f) * 3f },
            new RoomLightLook { name = "Corridor", keyColor = new Color(0.7f, 0.6f, 1f), keyIntensity = 3f, fillColor = new Color(0.4f, 0.3f, 0.8f), fillIntensity = 1.5f, rimIntensity = 2f, neonColor = new Color(0.6f, 0.3f, 1f) * 2.5f },
        };

        public bool AddMissingDefaultRooms()
        {
            var list = new System.Collections.Generic.List<RoomLightLook>(rooms ?? new RoomLightLook[0]);
            bool added = false;
            foreach (var d in DefaultRooms()) if (!list.Exists(r => r != null && r.name == d.name)) { list.Add(d); added = true; }
            if (added) rooms = list.ToArray();
            return added;
        }

        public RoomLightLook GetRoom(string roomName)
        {
            if (rooms != null) foreach (var r in rooms) if (r != null && r.name == roomName) return r;
            return rooms != null && rooms.Length > 0 ? rooms[0] : new RoomLightLook();
        }
    }
}

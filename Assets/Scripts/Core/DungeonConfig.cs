using System;
using UnityEngine;

namespace DungeonUnity
{
    /// One colour scheme per room (dance floor, bathroom, corridor...).
    [Serializable]
    public class RoomPalette
    {
        [Tooltip("Room name, matched by RoomZone.roomName.")] public string name = "Main Rave Hall";
        [Tooltip("Colours the rave lights cycle through.")] public Gradient lightColors = DungeonConfig.DefaultGradient();
        [Tooltip("Flat ambient light colour.")] public Color ambient = new Color(0.03f, 0.02f, 0.05f);
        [Tooltip("Laser beam colour in this room.")] public Color laserColor = Color.red;
        [Range(0f, 8f), Tooltip("Max rave light intensity.")] public float maxIntensity = 4f;
        [Tooltip("Lasers on while in this room.")] public bool lasersOn = true;
    }

    /// Project-wide tuning preset. Create via Assets > Create > DungeonUnity > Dungeon Config.
    [CreateAssetMenu(menuName = "DungeonUnity/Dungeon Config", fileName = "DungeonConfig")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("Tempo")]
        [Range(60f, 200f), Tooltip("Fallback tempo when no music is analysed.")] public float bpm = 128f;

        [Header("Audio (0-1, saved settings override these)")]
        [Range(0f, 1f)] public float masterVolume = 0.9f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;

        [Header("Features")]
        public bool enableLasers = true;
        public bool enablePostFX = true;
        public bool enableCrowd = true;
        public bool enableTrippyPills = true;
        public bool enableAutoPilot = true;
        [Tooltip("On Play: camera flies to the character, then self-play starts.")] public bool enableIntro = true;

        [Header("Crowd")]
        [Range(0, 40), Tooltip("Extra dancers spawned from the people models.")] public int crowdCount = 8;
        [Tooltip("Area (x,z half extents) the crowd is spread over.")] public Vector2 crowdArea = new Vector2(8f, 5f);

        [Header("Rooms")]
        [Tooltip("One palette per room. DungeonSetup adds any missing defaults by name and never overwrites yours.")]
        public RoomPalette[] rooms = DefaultRooms();

        [Header("Small props")]
        [Range(0.03f, 0.6f), Tooltip("Height of the pills lying on the floor (metres).")] public float pillHeight = 0.12f;
        [Range(0.1f, 2f), Tooltip("Scale for small floor props (pills, cans, bits).")] public float smallPropScale = 0.5f;

        public static RoomPalette[] DefaultRooms() => new[]
        {
            new RoomPalette { name = "Main Rave Hall" },
            new RoomPalette { name = "Red Light Corridor", lightColors = Solid(new Color(1f, 0.05f, 0.05f)), ambient = new Color(0.08f, 0.0f, 0.0f), laserColor = Color.red, maxIntensity = 2.5f, lasersOn = false },
            new RoomPalette { name = "Bathroom", lightColors = Solid(new Color(0.7f, 1f, 0.8f)), ambient = new Color(0.05f, 0.07f, 0.06f), laserColor = Color.green, maxIntensity = 1.5f, lasersOn = false },
            new RoomPalette { name = "Trippy Chill Room", lightColors = Pair(new Color(0.3f, 0.1f, 1f), new Color(0.1f, 1f, 0.8f)), ambient = new Color(0.04f, 0.02f, 0.08f), laserColor = new Color(0.5f, 0.2f, 1f), maxIntensity = 2f, lasersOn = false },
            new RoomPalette { name = "VIP Backstage", lightColors = Pair(new Color(1f, 0.75f, 0.2f), new Color(1f, 0.2f, 0.5f)), ambient = new Color(0.07f, 0.05f, 0.02f), laserColor = new Color(1f, 0.8f, 0.2f), maxIntensity = 3f, lasersOn = false },
            new RoomPalette { name = "Red Room", lightColors = Pair(new Color(1f, 0.02f, 0.02f), new Color(0.6f, 0f, 0f)), ambient = new Color(0.06f, 0f, 0f), laserColor = Color.red, maxIntensity = 5f, lasersOn = true },
            new RoomPalette { name = "Festival", lightColors = DefaultGradient(), ambient = new Color(0.04f, 0.03f, 0.08f), laserColor = Color.green, maxIntensity = 7f, lasersOn = true },
            new RoomPalette { name = "Corridor", lightColors = Solid(new Color(0.5f, 0.3f, 0.8f)), ambient = new Color(0.03f, 0.02f, 0.04f), laserColor = Color.magenta, maxIntensity = 1.5f, lasersOn = false },
        };

        /// Appends default palettes whose names are missing. Returns true if anything was added.
        public bool AddMissingDefaultRooms()
        {
            var list = new System.Collections.Generic.List<RoomPalette>(rooms ?? new RoomPalette[0]);
            bool added = false;
            foreach (var d in DefaultRooms()) if (!list.Exists(r => r != null && r.name == d.name)) { list.Add(d); added = true; }
            if (added) rooms = list.ToArray();
            return added;
        }

        public RoomPalette GetRoom(string roomName)
        {
            if (rooms == null || rooms.Length == 0) return null;
            foreach (var r in rooms) if (r != null && r.name == roomName) return r;
            return rooms[0];
        }

        void OnValidate()
        {
            bpm = Mathf.Clamp(bpm, 60f, 200f);
            crowdCount = Mathf.Clamp(crowdCount, 0, 40);
            crowdArea = Vector2.Max(crowdArea, Vector2.one);
        }

        public static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.magenta, 0f), new GradientColorKey(Color.cyan, 0.33f), new GradientColorKey(Color.red, 0.66f), new GradientColorKey(Color.magenta, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        public static Gradient Pair(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(a, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        public static Gradient Solid(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}

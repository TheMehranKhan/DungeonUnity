using System;
using UnityEngine;

namespace DungeonUnity
{
    /// One colour scheme per room (dance floor, bathroom, corridor...).
    [Serializable]
    public class RoomPalette
    {
        [Tooltip("Room name, matched by RoomLightingZone.")] public string name = "DanceFloor";
        [Tooltip("Colours the rave lights cycle through.")] public Gradient lightColors = DungeonConfig.DefaultGradient();
        [Tooltip("Flat ambient light colour.")] public Color ambient = new Color(0.03f, 0.02f, 0.05f);
        [Tooltip("Fog colour.")] public Color fogColor = new Color(0.05f, 0.02f, 0.08f);
        [Range(0f, 0.2f), Tooltip("Exponential-squared fog density.")] public float fogDensity = 0.04f;
        [Tooltip("Laser beam colour in this room.")] public Color laserColor = Color.red;
        [Range(0f, 8f), Tooltip("Max rave light intensity.")] public float maxIntensity = 4f;
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
        [Range(0f, 1f)] public float ambienceVolume = 0.4f;

        [Header("Features")]
        public bool enableLasers = true;
        public bool enableFog = true;
        public bool enableSmoke = true;
        public bool enablePostFX = true;
        public bool enableCrowd = true;
        public bool enableTrippyPills = true;
        public bool enableAutoPilot = true;

        [Header("Crowd")]
        [Range(0, 40), Tooltip("Extra dancers spawned from the people models.")] public int crowdCount = 8;
        [Tooltip("Area (x,z half extents) the crowd is spread over.")] public Vector2 crowdArea = new Vector2(8f, 5f);

        [Header("Rooms")]
        public RoomPalette[] rooms =
        {
            new RoomPalette(),
            new RoomPalette { name = "Bathroom", lightColors = Solid(new Color(0.7f, 1f, 0.8f)), ambient = new Color(0.05f, 0.07f, 0.06f), fogColor = new Color(0.04f, 0.06f, 0.05f), fogDensity = 0.02f, laserColor = Color.green, maxIntensity = 1.5f },
        };

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

        public static Gradient Solid(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}

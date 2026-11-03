using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonUnity
{
    /// Spawns imported character models as wandering NPCs. Each entry is auto-rigged (body + legs) with
    /// CharacterRig, walks with ProceduralWalk and wanders its room with AutoPilot.
    /// Dungeon > Import Characters fills the list from every GLB in Assets/Models/Characters.
    public class CharacterRoster : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string label = "Character";
            [Tooltip("Model prefab (GLB).")] public GameObject model;
            public bool enabled = true;
            [Tooltip("RoomZone.roomName to wander. Empty = any room.")] public string room = "";
            [Range(0, 10)] public int count = 1;
            [Range(0.3f, 4f)] public float height = 1.8f;
            [Range(-180f, 180f), Tooltip("Yaw fix if the model faces the wrong way.")] public float modelYaw = 0f;
            public CharacterRig.RigSource rigSource = CharacterRig.RigSource.Auto;
            [Range(0f, 0.9f), Tooltip("Crotch height fraction (0 = auto).")] public float legFraction = 0f;
            [Range(0.2f, 5f)] public float walkSpeed = 1.3f;
            [Range(0f, 20f)] public float dwellTime = 3f;
            [Tooltip("Dance in place instead of wandering. Auto = dance when the room is in Dance Rooms.")] public DanceMode dance = DanceMode.Auto;
        }

        public enum DanceMode { Auto, Dance, Wander }

        public CharacterRigConfig rigConfig;
        [Header("Dancing")]
        [Tooltip("NPCs placed in these rooms dance instead of wandering (when their entry is set to Auto).")] public string[] danceRooms = { "Main Rave Hall", "Festival" };
        [Range(60f, 200f)] public float danceBpm = 128f;
        [Tooltip("Adds a CharacterSpotlight to each spawned NPC (set by Improve Lighting & Rooms).")] public DungeonLightingConfig lightingConfig;
        public Entry[] characters = new Entry[0];
        [Range(2, 12), Tooltip("Random wander points per NPC.")] public int wanderPoints = 5;
        [Range(0f, 3f), Tooltip("Keep this far from the room walls.")] public float wallMargin = 1.5f;
        public bool spawnOnStart = true;
        [Tooltip("Random seed (0 = random every play).")] public int seed = 0;

        readonly List<GameObject> spawned = new List<GameObject>();

        void Start() { if (spawnOnStart) Spawn(); }

        [ContextMenu("Respawn")]
        public void Spawn()
        {
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear();
            var rng = seed != 0 ? new System.Random(seed) : new System.Random();
            var zones = FindObjectsOfType<RoomZone>();
            foreach (var e in characters)
            {
                if (e == null || !e.enabled || !e.model) continue;
                var zone = PickZone(zones, e.room, rng);
                for (int i = 0; i < e.count; i++) SpawnOne(e, zone, rng, i);
            }
        }

        RoomZone PickZone(RoomZone[] zones, string room, System.Random rng)
        {
            var list = new List<RoomZone>();
            foreach (var z in zones) if (z && z.GetComponent<BoxCollider>() && (string.IsNullOrEmpty(room) || z.roomName == room)) list.Add(z);
            return list.Count == 0 ? null : list[rng.Next(list.Count)];
        }

        Vector3 RandomPoint(RoomZone z, System.Random rng)
        {
            if (!z) return transform.position + new Vector3((float)rng.NextDouble() * 6f - 3f, 0f, (float)rng.NextDouble() * 6f - 3f);
            var b = z.GetComponent<BoxCollider>().bounds;
            float mx = Mathf.Min(wallMargin, b.extents.x * 0.8f), mz = Mathf.Min(wallMargin, b.extents.z * 0.8f);
            return new Vector3(Mathf.Lerp(b.min.x + mx, b.max.x - mx, (float)rng.NextDouble()), b.min.y + 0.05f, Mathf.Lerp(b.min.z + mz, b.max.z - mz, (float)rng.NextDouble()));
        }

        void SpawnOne(Entry e, RoomZone zone, System.Random rng, int index)
        {
            var go = new GameObject($"NPC_{e.label}_{index}");
            go.SetActive(false); // configure before Awake so CharacterRig builds with our settings
            go.transform.SetParent(transform, true);
            go.transform.position = RandomPoint(zone, rng);
            var cc = go.AddComponent<CharacterController>(); cc.height = e.height; cc.radius = Mathf.Clamp(e.height * 0.17f, 0.15f, 0.5f); cc.center = new Vector3(0, e.height * 0.5f, 0);
            var rig = go.AddComponent<CharacterRig>();
            rig.characterModel = e.model; rig.modelHeight = e.height; rig.modelYaw = e.modelYaw; rig.source = e.rigSource; rig.legFraction = e.legFraction; rig.config = rigConfig;
            go.AddComponent<ProceduralWalk>();
            if (!lightingConfig || lightingConfig.characterLight) go.AddComponent<CharacterSpotlight>().config = lightingConfig;
            bool dance = e.dance == DanceMode.Dance || (e.dance == DanceMode.Auto && zone && System.Array.IndexOf(danceRooms, zone.roomName) >= 0);
            if (dance)
            {
                go.transform.rotation = Quaternion.Euler(0f, rng.Next(0, 360), 0f);
                var d = go.AddComponent<ProceduralDance>(); d.bpm = danceBpm; d.variantSeed = rng.Next(); d.switchEveryBars = 2 + rng.Next(0, 4);
                d.intensity = 0.8f + (float)rng.NextDouble() * 0.4f;
                go.SetActive(true); spawned.Add(go); return;
            }
            var root = new GameObject(go.name + "_Route").transform; root.SetParent(transform, true);
            var pts = new Transform[wanderPoints];
            for (int i = 0; i < pts.Length; i++) { pts[i] = new GameObject("P" + i).transform; pts[i].SetParent(root, true); pts[i].position = RandomPoint(zone, rng); }
            var ap = go.AddComponent<AutoPilot>();
            ap.alwaysOn = true; ap.requireIdle = false; ap.selfPlayOnStart = false; ap.waypoints = pts; ap.order = AutoPilot.RouteOrder.Random;
            ap.speed = e.walkSpeed; ap.dwellTime = e.dwellTime; ap.openDoors = false;
            go.SetActive(true);
            spawned.Add(go); spawned.Add(root.gameObject);
        }
    }
}

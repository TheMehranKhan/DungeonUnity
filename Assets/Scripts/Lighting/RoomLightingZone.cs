using UnityEngine;

namespace DungeonUnity
{
    /// Trigger volume that switches the rave lighting and lasers to a room palette from DungeonConfig.
    [RequireComponent(typeof(BoxCollider))]
    public class RoomLightingZone : MonoBehaviour
    {
        public DungeonConfig config;
        [Tooltip("Room name in DungeonConfig.rooms.")] public string roomName = "Bathroom";
        [Tooltip("Room to restore on exit.")] public string exitRoomName = "DanceFloor";
        public string playerTag = "Player";
        public DungeonRaveLighting lighting;
        public LaserShowController[] lasers = new LaserShowController[0];

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) Apply(roomName); }
        void OnTriggerExit(Collider other) { if (other.CompareTag(playerTag)) Apply(exitRoomName); }

        void Apply(string room)
        {
            if (!config) return;
            var r = config.GetRoom(room); if (r == null) return;
            if (lighting) lighting.ApplyRoom(r);
            foreach (var l in lasers) if (l) l.SetColor(r.laserColor);
        }
    }
}

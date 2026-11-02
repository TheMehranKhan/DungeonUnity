using UnityEngine;

namespace DungeonUnity
{
    /// Walks a CharacterController (player or NPC) through waypoints. For the player it takes over
    /// after an idle timeout and hands control back as soon as there is input.
    public class AutoPilot : MonoBehaviour
    {
        public enum RouteOrder { Sequential, PingPong, Random }

        [Header("Route")]
        public Transform[] waypoints = new Transform[0];
        public RouteOrder order = RouteOrder.Sequential;
        [Range(0f, 30f), Tooltip("Seconds to wait at each point.")] public float dwellTime = 3f;
        [Range(0f, 10f), Tooltip("Random extra dwell.")] public float dwellJitter = 1f;
        [Range(0.1f, 3f)] public float arriveDistance = 0.6f;

        [Header("Movement")]
        [Range(0.2f, 10f)] public float speed = 2.2f;
        [Range(0.5f, 20f)] public float turnSpeed = 4f;
        [Tooltip("Look at this while dwelling (e.g. the DJ booth).")] public Transform lookAtWhileDwelling;

        [Header("Activation")]
        [Tooltip("Only drive the player after this many seconds without input.")] public bool requireIdle = true;
        [Range(1f, 300f)] public float idleTimeout = 20f;
        [Tooltip("Start driving immediately (NPCs).")] public bool alwaysOn = false;

        PlayerModeController player; CharacterController cc;
        int index, dir = 1; float dwellUntil; bool dwelling;
        public bool Driving { get; private set; }

        void Awake() { player = GetComponent<PlayerModeController>(); cc = GetComponent<CharacterController>(); }

        void Update()
        {
            if (waypoints == null || waypoints.Length == 0) { Stop(); return; }
            bool want = alwaysOn || (player && player.Active && !player.Paused && (!requireIdle || InputCompat.IdleSeconds > idleTimeout));
            if (!want) { Stop(); return; }
            if (!Driving) { Driving = true; if (player) player.ExternalControl = true; }

            var target = waypoints[Mathf.Clamp(index, 0, waypoints.Length - 1)];
            if (!target) { Advance(); return; }
            Vector3 to = target.position - transform.position; to.y = 0f;
            if (dwelling)
            {
                Face(lookAtWhileDwelling ? lookAtWhileDwelling.position - transform.position : target.forward, 0f);
                if (Time.time >= dwellUntil) { dwelling = false; Advance(); }
                return;
            }
            if (to.magnitude <= arriveDistance) { dwelling = true; dwellUntil = Time.time + dwellTime + Random.Range(0f, dwellJitter); return; }
            Face(to, speed);
        }

        void Face(Vector3 dirVec, float moveSpeed)
        {
            dirVec.y = 0f;
            Quaternion rot = dirVec.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dirVec) : transform.rotation;
            Vector3 vel = moveSpeed > 0f ? dirVec.normalized * moveSpeed : Vector3.zero;
            if (player) player.DriveExternally(vel, rot, turnSpeed);
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
                if (cc && cc.enabled) cc.SimpleMove(vel); else transform.position += vel * Time.deltaTime;
            }
        }

        void Advance()
        {
            int n = waypoints.Length; if (n <= 1) { index = 0; return; }
            switch (order)
            {
                case RouteOrder.Random: int r; do r = Random.Range(0, n); while (r == index); index = r; break;
                case RouteOrder.PingPong: if (index + dir >= n || index + dir < 0) dir = -dir; index += dir; break;
                default: index = (index + 1) % n; break;
            }
        }

        void Stop()
        {
            if (!Driving) return;
            Driving = false; dwelling = false;
            if (player) player.ExternalControl = false;
        }
    }
}

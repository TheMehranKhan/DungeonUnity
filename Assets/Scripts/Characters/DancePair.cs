using UnityEngine;

namespace DungeonUnity
{
    /// Spawns two rigged characters (MaskedRaver and SpikeQueen by default) that dance together on a room's floor:
    /// they face each other, hit the same beat with mirrored moves and now and then circle around each other.
    /// The models come from the fields below, or from CharacterRoster entries matched by label. Those roster
    /// entries are switched off so the same characters don't also wander.
    public class DancePair : MonoBehaviour
    {
        [Header("Who")]
        public GameObject modelA; public GameObject modelB;
        [Tooltip("Used when modelA is empty: CharacterRoster entry label.")] public string labelA = "MaskedRaver";
        [Tooltip("Used when modelB is empty: CharacterRoster entry label.")] public string labelB = "SpikeQueen";
        [Range(0.3f, 4f)] public float height = 1.8f;
        [Range(-180f, 180f)] public float modelYawA = 0f, modelYawB = 0f;
        public CharacterRigConfig rigConfig;
        public DungeonLightingConfig lightingConfig;
        [Tooltip("Turn off the roster entries for these two so they only dance here.")] public bool takeFromRoster = true;

        [Header("Where")]
        [Tooltip("RoomZone.roomName of the dance floor.")] public string room = "Main Rave Hall";
        [Tooltip("Offset from the room centre (on the floor).")] public Vector3 centerOffset = new Vector3(-3.5f, 0f, -1.5f);
        [Range(0.6f, 4f), Tooltip("Distance between the two dancers.")] public float spacing = 1.3f;

        [Header("How")]
        [Range(60f, 200f)] public float bpm = 128f;
        [Tooltip("Mirror B's side-to-side moves.")] public bool mirror = true;
        [Range(0, 32), Tooltip("Circle around each other every N bars (0 = never).")] public int circleEveryBars = 8;
        [Range(1, 8), Tooltip("Bars one circle takes.")] public int circleBars = 2;
        [Range(-360f, 360f), Tooltip("Degrees turned per circle.")] public float circleDegrees = 180f;
        [Range(0, 16)] public int switchEveryBars = 4;
        public int variantSeed = 7;

        [Range(-180f, 180f), Tooltip("Facing axis of the pair on the floor (0 = side by side along X).")] public float startAngle = 0f;

        Transform a, b; Vector3 center; float angle;

        void Awake()
        {
            var roster = FindObjectOfType<CharacterRoster>();
            if (roster && roster.characters != null)
                foreach (var e in roster.characters)
                {
                    if (e == null) continue;
                    if (!modelA && e.label == labelA) { modelA = e.model; height = e.height; modelYawA = e.modelYaw; }
                    else if (!modelB && e.label == labelB) { modelB = e.model; modelYawB = e.modelYaw; }
                    if (takeFromRoster && (e.label == labelA || e.label == labelB || (e.model && (e.model == modelA || e.model == modelB)))) e.enabled = false;
                    if (!rigConfig) rigConfig = roster.rigConfig;
                    if (!lightingConfig) lightingConfig = roster.lightingConfig;
                }
        }

        void Start()
        {
            center = transform.position;
            foreach (var z in FindObjectsOfType<RoomZone>())
                if (z.roomName == room && z.GetComponent<BoxCollider>()) { var bb = z.GetComponent<BoxCollider>().bounds; center = new Vector3(bb.center.x, bb.min.y, bb.center.z); break; }
            center += centerOffset;
            a = Spawn("Dancer_" + labelA, modelA, modelYawA, false);
            b = Spawn("Dancer_" + labelB, modelB, modelYawB, mirror);
            angle = startAngle; Place();
        }

        Transform Spawn(string n, GameObject model, float yaw, bool mir)
        {
            var go = new GameObject(n); go.SetActive(false); go.transform.SetParent(transform, true); go.transform.position = center;
            var rig = go.AddComponent<CharacterRig>(); rig.characterModel = model; rig.modelHeight = height; rig.modelYaw = yaw; rig.config = rigConfig;
            if (!model) rig.source = CharacterRig.RigSource.GeneratedLegs;
            go.AddComponent<ProceduralWalk>();
            var d = go.AddComponent<ProceduralDance>(); d.bpm = bpm; d.mirror = mir; d.variantSeed = variantSeed; d.switchEveryBars = switchEveryBars;
            if (!lightingConfig || lightingConfig.characterLight) go.AddComponent<CharacterSpotlight>().config = lightingConfig;
            go.SetActive(true);
            return go.transform;
        }

        void Update()
        {
            if (!a || !b) return;
            float beats = ProceduralDance.BeatTime(bpm); float bars = beats / 4f;
            if (circleEveryBars > 0)
            {
                float into = Mathf.Repeat(bars, circleEveryBars);
                int done = Mathf.FloorToInt(bars / circleEveryBars);
                float t = Mathf.Clamp01((into - (circleEveryBars - circleBars)) / circleBars);
                angle = startAngle + (done + Mathf.SmoothStep(0f, 1f, t)) * circleDegrees;
            }
            Place();
        }

        void Place()
        {
            var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.right * (spacing * 0.5f);
            a.position = center - dir; b.position = center + dir;
            a.rotation = Quaternion.LookRotation(b.position - a.position, Vector3.up);
            b.rotation = Quaternion.LookRotation(a.position - b.position, Vector3.up);
        }

        void OnDrawGizmosSelected() { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position + centerOffset, spacing * 0.5f); }
    }
}

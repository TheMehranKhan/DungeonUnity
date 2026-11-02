using UnityEngine;

namespace DungeonUnity
{
    /// Rigs a character for ProceduralWalk. Drop any model in the Character slot: a rigged model (bones),
    /// a static GLB made of separate body/leg meshes, or a single static body+legs mesh (split automatically).
    /// With no model it falls back to simple generated capsule legs. Use Dungeon > Character Rig Setup to rig in the editor.
    public class CharacterRig : MonoBehaviour
    {
        public enum RigSource { Auto, ExistingBones, StaticMeshParts, SplitSingleMesh, GeneratedLegs }

        [Header("Character")]
        [Tooltip("Model prefab/GLB or a child object. Prefabs are instantiated under this object on Build.")] public GameObject characterModel;
        [Range(0f, 4f), Tooltip("Scale the model to this height in metres (0 = keep).")] public float modelHeight = 1.8f;
        [Range(-180f, 180f), Tooltip("Yaw correction if the model faces the wrong way.")] public float modelYaw = 0f;
        public Vector3 modelOffset = Vector3.zero;
        public RigSource source = RigSource.Auto;

        [Header("Bones / parts (auto-detected by name when empty)")]
        public Transform pelvis;
        [Tooltip("Body visual bobbed and leaned by ProceduralWalk. Defaults to the model root.")] public Transform body;
        public Transform leftUpperLeg, leftLowerLeg, rightUpperLeg, rightLowerLeg;

        [Header("Static mesh split")]
        [Range(0f, 0.9f), Tooltip("Crotch height as fraction of model height (0 = auto detect).")] public float legFraction = 0f;
        public bool splitKnees = true;
        [Range(0.1f, 0.9f), Tooltip("Knee height as a fraction of the leg.")] public float kneeFraction = 0.5f;

        [Header("Generated legs fallback")]
        public CharacterRigConfig config;
        [Tooltip("Height of the hips above this transform's origin. 0 = use leg length.")] [Range(0f, 2f)] public float hipHeight = 0f;
        [Tooltip("Build on Awake if not rigged yet.")] public bool buildOnAwake = true;
        [Tooltip("Optional material for generated legs.")] public Material legMaterial;

        public Transform LeftHip => leftUpperLeg;
        public Transform RightHip => rightUpperLeg;
        public Transform LeftKnee => leftLowerLeg;
        public Transform RightKnee => rightLowerLeg;
        public Transform Body => body ? body : transform;
        /// Bumped on every Build so animators re-cache rest poses.
        public int Version { get; private set; }
        public bool IsRigged => leftUpperLeg && rightUpperLeg;
        public GameObject ModelInstance { get; private set; }

        void Awake() { if (buildOnAwake && !IsRigged) Build(); }

        /// Instantiates the model if needed, then finds or creates leg joints. Returns the split result (if any) so the editor can save meshes.
        public RigUtility.SplitResult Build()
        {
            RigUtility.SplitResult split = null;
            var model = EnsureModel();
            if (model)
            {
                if (!body) body = model.transform;
                var d = RigUtility.Detect(model.transform);
                if (!pelvis) pelvis = d.pelvis;
                if (!leftUpperLeg) leftUpperLeg = d.leftUpper; if (!leftLowerLeg) leftLowerLeg = d.leftLower;
                if (!rightUpperLeg) rightUpperLeg = d.rightUpper; if (!rightLowerLeg) rightLowerLeg = d.rightLower;

                bool bones = IsRigged && RigUtility.IsBone(leftUpperLeg);
                if (source == RigSource.StaticMeshParts || (source == RigSource.Auto && IsRigged && !bones))
                {
                    if (RigUtility.HasRenderer(leftUpperLeg)) { var p = RigUtility.WrapInPivot(leftUpperLeg, leftUpperLeg.parent, "Hip_L"); leftUpperLeg = p; }
                    if (RigUtility.HasRenderer(rightUpperLeg)) { var p = RigUtility.WrapInPivot(rightUpperLeg, rightUpperLeg.parent, "Hip_R"); rightUpperLeg = p; }
                    if (RigUtility.HasRenderer(leftLowerLeg)) leftLowerLeg = WrapLower(leftLowerLeg, leftUpperLeg, "Knee_L");
                    if (RigUtility.HasRenderer(rightLowerLeg)) rightLowerLeg = WrapLower(rightLowerLeg, rightUpperLeg, "Knee_R");
                }
                else if (!IsRigged && (source == RigSource.Auto || source == RigSource.SplitSingleMesh))
                {
                    var mf = model.GetComponentInChildren<MeshFilter>();
                    if (mf) split = RigUtility.SplitStaticMesh(model.transform, mf, legFraction, splitKnees, kneeFraction);
                    if (split != null)
                    {
                        leftUpperLeg = split.leftUpper; rightUpperLeg = split.rightUpper;
                        leftLowerLeg = split.leftLower; rightLowerLeg = split.rightLower;
                    }
                    else Debug.LogWarning("[CharacterRig] Could not split mesh (enable Read/Write on the model import). Using generated legs.", this);
                }
            }
            if (!IsRigged || source == RigSource.GeneratedLegs) BuildGeneratedLegs();
            Version++;
            return split;
        }

        Transform WrapLower(Transform lower, Transform hip, string name)
        {
            var k = RigUtility.WrapInPivot(lower, lower.parent, name);
            if (hip && !k.IsChildOf(hip)) k.SetParent(hip, true);
            return k;
        }

        GameObject EnsureModel()
        {
            if (!characterModel) return null;
            if (characterModel.scene.IsValid()) { ModelInstance = characterModel; return characterModel; }
            var inst = Instantiate(characterModel, transform);
            inst.name = characterModel.name;
            inst.transform.localRotation = Quaternion.Euler(0f, modelYaw, 0f);
            inst.transform.localPosition = modelOffset;
            if (modelHeight > 0f)
            {
                var b = RigUtility.RendererBounds(inst.transform);
                if (b.size.y > 1e-3f) inst.transform.localScale *= modelHeight / b.size.y;
                b = RigUtility.RendererBounds(inst.transform);
                inst.transform.position += Vector3.up * (transform.position.y - b.min.y);
            }
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
            characterModel = inst; ModelInstance = inst;
            return inst;
        }

        void BuildGeneratedLegs()
        {
            if (!config) return;
            leftUpperLeg = transform.Find("Hip_L"); rightUpperLeg = transform.Find("Hip_R");
            if (!config.generateLegs) return;
            float h = hipHeight > 0f ? hipHeight : config.legLength;
            if (!leftUpperLeg) leftUpperLeg = MakeLeg("Hip_L", -config.hipWidth * 0.5f, h);
            if (!rightUpperLeg) rightUpperLeg = MakeLeg("Hip_R", config.hipWidth * 0.5f, h);
            leftLowerLeg = null; rightLowerLeg = null;
        }

        Transform MakeLeg(string name, float x, float h)
        {
            var hip = new GameObject(name).transform;
            hip.SetParent(transform, false); hip.localPosition = new Vector3(x, h, 0f);
            var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = name + "_Leg";
            var col = leg.GetComponent<Collider>(); if (col) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            leg.transform.SetParent(hip, false);
            leg.transform.localPosition = new Vector3(0f, -config.legLength * 0.5f, 0f);
            leg.transform.localScale = new Vector3(config.legThickness, config.legLength * 0.5f, config.legThickness);
            var r = leg.GetComponent<Renderer>();
            if (r)
            {
                if (legMaterial) r.sharedMaterial = legMaterial;
                else { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", config.legColor); mpb.SetColor("_BaseColor", config.legColor); r.SetPropertyBlock(mpb); }
            }
            return hip;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var t in new[] { leftUpperLeg, leftLowerLeg, rightUpperLeg, rightLowerLeg, pelvis }) if (t) Gizmos.DrawWireSphere(t.position, 0.05f);
        }
    }
}

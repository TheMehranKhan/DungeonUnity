using UnityEngine;

namespace DungeonUnity
{
    /// Generates simple capsule legs (hip pivots) under a character model using a CharacterRigConfig.
    public class CharacterRig : MonoBehaviour
    {
        public CharacterRigConfig config;
        [Tooltip("Height of the hips above this transform's origin. 0 = use leg length.")] [Range(0f, 2f)] public float hipHeight = 0f;
        [Tooltip("Build legs on Awake if missing.")] public bool buildOnAwake = true;
        [Tooltip("Optional material for the legs.")] public Material legMaterial;

        public Transform LeftHip { get; private set; }
        public Transform RightHip { get; private set; }
        public Transform Body => transform;

        void Awake() { if (buildOnAwake) Build(); }

        public void Build()
        {
            LeftHip = transform.Find("Hip_L"); RightHip = transform.Find("Hip_R");
            if (!config || !config.generateLegs) return;
            float h = hipHeight > 0f ? hipHeight : config.legLength;
            if (!LeftHip) LeftHip = MakeLeg("Hip_L", -config.hipWidth * 0.5f, h);
            if (!RightHip) RightHip = MakeLeg("Hip_R", config.hipWidth * 0.5f, h);
        }

        Transform MakeLeg(string name, float x, float h)
        {
            var hip = new GameObject(name).transform;
            hip.SetParent(transform, false); hip.localPosition = new Vector3(x, h, 0f);
            var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = name + "_Leg";
            var col = leg.GetComponent<Collider>(); if (col) Destroy(col);
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
    }
}

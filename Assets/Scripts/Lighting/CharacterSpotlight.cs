using UnityEngine;

namespace DungeonUnity
{
    /// Small shadowless light that follows a character so it reads in dark rooms. Also lifts unlit/black materials to URP Lit.
    [DisallowMultipleComponent]
    public class CharacterSpotlight : MonoBehaviour
    {
        [Tooltip("Preset; empty = use the values below.")] public DungeonLightingConfig config;
        public Color color = new Color(1f, 0.85f, 0.95f);
        [Range(0f, 5f)] public float intensity = 0.9f;
        [Range(0.5f, 8f)] public float range = 3f;
        public Vector3 offset = new Vector3(0.4f, 2.2f, -0.8f);
        public bool fixMaterials = true;

        Light lightRef;

        void Start()
        {
            if (config)
            {
                if (!config.characterLight) { enabled = false; return; }
                color = config.characterLightColor; intensity = config.characterLightIntensity; range = config.characterLightRange;
                offset = config.characterLightOffset; fixMaterials = config.fixCharacterMaterials;
            }
            var go = new GameObject("CharacterLight");
            go.transform.SetParent(transform, false);
            lightRef = go.AddComponent<Light>();
            lightRef.type = LightType.Point; lightRef.shadows = LightShadows.None;
            if (fixMaterials) FixMaterials();
            Apply();
        }

        void OnValidate() { if (lightRef) Apply(); }

        void Apply()
        {
            lightRef.color = color; lightRef.intensity = intensity; lightRef.range = range;
            lightRef.transform.localPosition = offset;
        }

        void FixMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!lit) return;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r.GetComponent<Light>()) continue;
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (!m || !m.shader) continue;
                    var sn = m.shader.name;
                    bool broken = sn == "Hidden/InternalErrorShader" || sn == "Standard" || sn.StartsWith("Legacy Shaders/") || (sn.Contains("Unlit") && !sn.Contains("glTF"));
                    if (!broken) continue;
                    Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                    if (!tex && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
                    Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    if (col.maxColorComponent < 0.05f) col = Color.white;
                    var n = new Material(lit) { name = m.name + "_Lit" };
                    if (tex) n.SetTexture("_BaseMap", tex);
                    n.SetColor("_BaseColor", col);
                    mats[i] = n;
                }
                r.materials = mats;
            }
        }
    }
}

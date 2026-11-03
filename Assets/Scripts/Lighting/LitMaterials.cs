using UnityEngine;

namespace DungeonUnity
{
    /// Creates materials on the URP Lit shader (falls back to Standard only when URP is missing), so nothing renders magenta.
    public static class LitMaterials
    {
        public static Shader LitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        /// Dark base colour with HDR emission, for neon strips and door trims.
        public static Material Emissive(string name, Color emission)
        {
            var m = new Material(LitShader()) { name = name };
            SetEmissive(m, emission);
            return m;
        }

        public static Material Solid(string name, Color baseColor, float smoothness = 0.4f, float metallic = 0f)
        {
            var m = new Material(LitShader()) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static void SetEmissive(Material m, Color emission)
        {
            if (!m) return;
            var dark = new Color(0.02f, 0.02f, 0.02f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", dark);
            if (m.HasProperty("_Color")) m.SetColor("_Color", dark);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
        }
    }
}

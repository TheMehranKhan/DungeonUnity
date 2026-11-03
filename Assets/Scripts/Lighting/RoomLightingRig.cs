using System.Collections.Generic;
using UnityEngine;

namespace DungeonUnity
{
    /// Key / fill / rim lights, neon strips, a reflection probe and light probes for one room.
    /// Sits on a RoomZone (uses its BoxCollider as the room bounds). Rebuild from the context menu after tweaking.
    [DisallowMultipleComponent]
    public class RoomLightingRig : MonoBehaviour
    {
        [Tooltip("Preset. Empty = use the values below as they are.")] public DungeonLightingConfig config;
        [Tooltip("Overrides the preset look when on.")] public bool customLook = false;
        public RoomLightLook look = new RoomLightLook();

        [Header("Placement")]
        [Range(0.05f, 1f), Tooltip("Strip height above the floor.")] public float floorStripHeight = 0.15f;
        [Range(0.02f, 0.3f)] public float stripThickness = 0.06f;
        [Range(0f, 1f), Tooltip("Inset from the walls.")] public float stripInset = 0.3f;
        public bool ceilingStrips = true;
        public bool floorStrips = true;

        [Header("Generated (read only)")]
        public Light key, fill, rim;
        public Transform generatedRoot;
        public Material neonMaterial, neonMaterial2;

        readonly List<Light> stripLights = new List<Light>();

        public RoomLightLook Look
        {
            get
            {
                if (customLook || !config) return look;
                var z = GetComponent<RoomZone>();
                return config.GetRoom(z ? z.roomName : name);
            }
        }

        Bounds RoomBounds()
        {
            var b = GetComponent<BoxCollider>();
            if (b) return new Bounds(transform.TransformPoint(b.center), Vector3.Scale(b.size, transform.lossyScale));
            return new Bounds(transform.position, new Vector3(10, 5, 10));
        }

        [ContextMenu("Rebuild Lighting")]
        public void Build()
        {
            Clear();
            var L = Look; var b = RoomBounds();
            float scale = config ? config.globalLightScale : 1f;
            generatedRoot = new GameObject("LightRig").transform; generatedRoot.SetParent(transform, false);
            generatedRoot.position = b.center;
            float h = b.size.y, diag = new Vector2(b.size.x, b.size.z).magnitude;

            key = MakeLight("Key", LightType.Spot, new Vector3(b.center.x - b.extents.x * 0.3f, b.max.y - 0.2f, b.center.z - b.extents.z * 0.4f), L.keyColor, L.keyIntensity * scale, diag * 1.2f);
            key.spotAngle = L.keyAngle; key.transform.LookAt(new Vector3(b.center.x, b.min.y, b.center.z + b.extents.z * 0.2f));
            key.shadows = config ? config.keyShadows : LightShadows.Soft; key.shadowStrength = config ? config.shadowStrength : 0.75f;

            fill = MakeLight("Fill", LightType.Point, new Vector3(b.center.x + b.extents.x * 0.35f, b.min.y + h * 0.55f, b.center.z), L.fillColor, L.fillIntensity * scale, diag * 0.9f);
            fill.shadows = LightShadows.None;

            rim = MakeLight("Rim", LightType.Spot, new Vector3(b.center.x + b.extents.x * 0.2f, b.max.y - 0.3f, b.max.z - 0.5f), L.rimColor, L.rimIntensity * scale, diag);
            rim.spotAngle = 110f; rim.transform.LookAt(new Vector3(b.center.x, b.min.y + 1f, b.center.z - b.extents.z * 0.3f)); rim.shadows = LightShadows.None;

            if (config && config.roomFill && config.roomFillIntensity > 0f)
            {
                var bf = MakeLight("BaseFill", LightType.Point, new Vector3(b.center.x, b.max.y - 0.4f, b.center.z), config.roomFillColor, config.roomFillIntensity, diag * config.roomFillRange);
                bf.shadows = LightShadows.None; bf.renderMode = LightRenderMode.Auto;
            }

            if (L.neonStrips) BuildStrips(b, L, scale);
            if (!config || config.reflectionProbes) BuildReflectionProbe(b);
            if (!config || config.lightProbes) BuildLightProbes(b);
        }

        Light MakeLight(string n, LightType type, Vector3 pos, Color c, float intensity, float range)
        {
            var go = new GameObject(n); go.transform.SetParent(generatedRoot, true); go.transform.position = pos;
            var l = go.AddComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; l.range = range;
            l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        void BuildStrips(Bounds b, RoomLightLook L, float scale)
        {
            if (!neonMaterial) neonMaterial = LitMaterials.Emissive("Neon_" + name, L.neonColor);
            else LitMaterials.SetEmissive(neonMaterial, L.neonColor);
            if (!neonMaterial2) neonMaterial2 = LitMaterials.Emissive("Neon2_" + name, L.neonColor2);
            else LitMaterials.SetEmissive(neonMaterial2, L.neonColor2);
            float t = stripThickness, ins = stripInset;
            if (floorStrips) Perimeter(b, b.min.y + floorStripHeight, t, ins, neonMaterial, L.neonColor, L.stripLightAmount * scale);
            if (ceilingStrips) Perimeter(b, b.max.y - 0.15f, t, ins, neonMaterial2, L.neonColor2, floorStrips ? 0f : L.stripLightAmount * scale);
        }

        void Perimeter(Bounds b, float y, float t, float ins, Material m, Color c, float lightAmount)
        {
            float x0 = b.min.x + ins, x1 = b.max.x - ins, z0 = b.min.z + ins, z1 = b.max.z - ins;
            Strip(new Vector3((x0 + x1) / 2, y, z0), new Vector3(x1 - x0, t, t), m);
            Strip(new Vector3((x0 + x1) / 2, y, z1), new Vector3(x1 - x0, t, t), m);
            Strip(new Vector3(x0, y, (z0 + z1) / 2), new Vector3(t, t, z1 - z0), m);
            Strip(new Vector3(x1, y, (z0 + z1) / 2), new Vector3(t, t, z1 - z0), m);
            if (lightAmount <= 0f) return;
            float mx = Mathf.Max(0.001f, Mathf.Max(c.r, Mathf.Max(c.g, c.b))); var lc = new Color(c.r / mx, c.g / mx, c.b / mx);
            float spacing = 6f;
            for (float x = x0; x <= x1 + 0.01f; x += spacing) { StripLight(new Vector3(x, y, z0 + 0.2f), lc, lightAmount); StripLight(new Vector3(x, y, z1 - 0.2f), lc, lightAmount); }
            for (float z = z0 + spacing; z < z1 - 0.01f; z += spacing) { StripLight(new Vector3(x0 + 0.2f, y, z), lc, lightAmount); StripLight(new Vector3(x1 - 0.2f, y, z), lc, lightAmount); }
        }

        void Strip(Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "NeonStrip";
            var col = go.GetComponent<Collider>(); if (col) DestroyNow(col);
            go.transform.SetParent(generatedRoot, true); go.transform.position = pos; go.transform.localScale = size;
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void StripLight(Vector3 pos, Color c, float amount)
        {
            var l = MakeLight("StripLight", LightType.Point, pos, c, 1.5f * amount, 3.5f);
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.Auto; stripLights.Add(l);
        }

        void BuildReflectionProbe(Bounds b)
        {
            var go = new GameObject("ReflectionProbe"); go.transform.SetParent(generatedRoot, true); go.transform.position = b.center;
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            p.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            p.size = b.size; p.boxProjection = true; p.intensity = 1f;
            p.resolution = config ? Mathf.ClosestPowerOfTwo(config.reflectionResolution) : 128;
        }

        void BuildLightProbes(Bounds b)
        {
            float s = config ? Mathf.Max(1f, config.lightProbeSpacing) : 4f;
            var pts = new List<Vector3>();
            for (float x = b.min.x + 0.5f; x <= b.max.x - 0.5f + 0.01f; x += s)
                for (float z = b.min.z + 0.5f; z <= b.max.z - 0.5f + 0.01f; z += s)
                    foreach (var y in new[] { b.min.y + 0.5f, b.min.y + Mathf.Min(2.2f, b.size.y - 0.3f) })
                        pts.Add(new Vector3(x, y, z) - b.center);
            if (pts.Count < 4) return;
            var go = new GameObject("LightProbes"); go.transform.SetParent(generatedRoot, false); go.transform.position = b.center;
            go.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
        }

        [ContextMenu("Clear Lighting")]
        public void Clear()
        {
            stripLights.Clear();
            var old = new List<GameObject>();
            foreach (Transform c in transform) if (c.name == "LightRig") old.Add(c.gameObject);
            foreach (var o in old) { o.transform.SetParent(null); DestroyNow(o); }
            generatedRoot = null; key = fill = rim = null;
        }

        static void DestroyNow(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
    }
}

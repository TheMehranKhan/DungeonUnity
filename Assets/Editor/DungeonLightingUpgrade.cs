#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonUnity.Editor
{
    /// Makes the dungeon look good in URP: per-room key/fill/rim lights, neon strips, reflection and light probes,
    /// brighter trilight ambient, bloom + ACES tonemapping + colour, door frames with lit trims and doorway triggers.
    /// Removes any leftover smoke/fog. Tweak everything in Assets/Settings/DungeonLightingConfig.asset, then run again.
    /// Menu: Dungeon/Improve Lighting & Rooms. Build Scene runs it automatically.
    public static class DungeonLightingUpgrade
    {
        const string SettingsPath = "Assets/Settings/DungeonLightingConfig.asset";
        const string MatDir = "Assets/Materials/Lighting";
        const string DungeonScene = "Assets/Scenes/Dungeon.unity";

        [MenuItem("Dungeon/Improve Lighting & Rooms")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("Dungeon.unity"))
            {
                if (!File.Exists(DungeonScene)) { Debug.LogWarning("[DungeonLighting] Run Dungeon > Build Scene first."); return; }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(DungeonScene);
            }
            ApplyToOpenScene();
        }

        /// Opens the scene at path, upgrades it, saves it. Used by Build Scene.
        public static void ApplyToScene(string path)
        {
            EditorSceneManager.OpenScene(path);
            ApplyToOpenScene();
        }

        public static DungeonLightingConfig LoadConfig()
        {
            Directory.CreateDirectory("Assets/Settings");
            var c = AssetDatabase.LoadAssetAtPath<DungeonLightingConfig>(SettingsPath);
            if (!c) { c = ScriptableObject.CreateInstance<DungeonLightingConfig>(); AssetDatabase.CreateAsset(c, SettingsPath); }
            if (c.AddMissingDefaultRooms()) EditorUtility.SetDirty(c);
            return c;
        }

        static void ApplyToOpenScene()
        {
            var cfg = LoadConfig();
            Directory.CreateDirectory(MatDir);
            int removed = RemoveSmoke();

            // Ambient + fog
            RenderSettings.fog = false;
            foreach (var rave in Object.FindObjectsOfType<DungeonRaveLighting>())
            {
                rave.ambientBoost = cfg.ambientBoost; rave.trilight = cfg.trilightAmbient;
                rave.baseAmbient = cfg.baseAmbient; rave.baseIntensity = cfg.baseAmbientIntensity;
                rave.baseSky = cfg.baseSkyColor; rave.baseEquator = cfg.baseEquatorColor; rave.baseGround = cfg.baseGroundColor;
                rave.minIntensity = Mathf.Max(rave.minIntensity, cfg.raveMinIntensity);
                if (rave.maxIntensity < rave.minIntensity) rave.maxIntensity = rave.minIntensity + 1f;
                EditorUtility.SetDirty(rave);
            }
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (!l.transform.parent || l.transform.parent.name != "LightRig") { l.shadows = cfg.raveLightShadows ? LightShadows.Soft : LightShadows.None; }

            // Editor-time ambient floor (runtime DungeonRaveLighting keeps it too)
            if (cfg.baseAmbient)
            {
                float bi = cfg.baseAmbientIntensity;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = cfg.baseSkyColor * bi * 2f; RenderSettings.ambientEquatorColor = cfg.baseEquatorColor * bi * 2f; RenderSettings.ambientGroundColor = cfg.baseGroundColor * bi * 2f;
                RenderSettings.ambientIntensity = 1f;
            }

            // Character lights: player, scene NPCs, and the roster spawns
            int charLights = 0;
            var chars = new HashSet<GameObject>();
            foreach (var r in Object.FindObjectsOfType<CharacterRig>(true)) chars.Add(r.gameObject);
            foreach (var d in Object.FindObjectsOfType<CrowdDancer>(true)) chars.Add(d.gameObject);
            foreach (var p in GameObject.FindGameObjectsWithTag("Player")) chars.Add(p);
            foreach (var go in chars)
            {
                var cs = go.GetComponent<CharacterSpotlight>();
                if (!cfg.characterLight) { if (cs) Object.DestroyImmediate(cs); continue; }
                if (!cs) cs = go.AddComponent<CharacterSpotlight>();
                cs.config = cfg; EditorUtility.SetDirty(cs); charLights++;
            }
            foreach (var roster in Object.FindObjectsOfType<CharacterRoster>(true)) { roster.lightingConfig = cfg; EditorUtility.SetDirty(roster); }

            // Post FX volume
            foreach (var post in Object.FindObjectsOfType<PostFXSetup>())
            {
                post.bloom = true; post.bloomIntensity = cfg.bloomIntensity; post.bloomThreshold = cfg.bloomThreshold; post.bloomScatter = cfg.bloomScatter;
                post.tonemapping = cfg.tonemapping != "None"; post.tonemappingMode = cfg.tonemapping; post.postExposure = Mathf.Clamp(cfg.postExposure, 0f, 1f);
                post.colorAdjustments = true; post.saturation = cfg.saturation; post.contrast = cfg.contrast;
                post.vignette = cfg.vignette > 0f; post.vignetteIntensity = cfg.vignette;
                post.cameras = Object.FindObjectsOfType<Camera>(true);
                EditorUtility.SetDirty(post);
            }

            // Rooms
            int rooms = 0;
            foreach (var zone in Object.FindObjectsOfType<RoomZone>())
            {
                if (!zone.GetComponent<BoxCollider>()) continue;
                var rig = zone.GetComponent<RoomLightingRig>(); if (!rig) rig = zone.gameObject.AddComponent<RoomLightingRig>();
                rig.config = cfg;
                var look = rig.Look;
                rig.neonMaterial = EmissiveAsset("Neon_" + Safe(zone.roomName) + "_" + zone.name, look.neonColor);
                rig.neonMaterial2 = EmissiveAsset("Neon2_" + Safe(zone.roomName) + "_" + zone.name, look.neonColor2);
                rig.Build(); EditorUtility.SetDirty(rig); rooms++;
            }

            // Doors and doorways
            int doors = 0;
            if (cfg.doorFrames)
            {
                var frameMat = SolidAsset("DoorFrame_Metal", new Color(0.08f, 0.07f, 0.1f), 0.7f, 0.8f);
                var trimMat = EmissiveAsset("DoorTrim", cfg.doorTrimClosed);
                foreach (var old in Object.FindObjectsOfType<Transform>(true).Where(t => t && t.name.StartsWith("DoorFrame_")).ToList())
                    if (old) Object.DestroyImmediate(old.gameObject);

                var doorBounds = new List<Bounds>();
                foreach (var door in Object.FindObjectsOfType<BathroomDoor>())
                {
                    var rs = door.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    doorBounds.Add(b);
                    door.speed = 2f / Mathf.Max(0.1f, cfg.doorOpenSeconds);
                    door.swing = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(0.85f, 1.04f), new Keyframe(1, 1, 0, 0));
                    if (cfg.doorwayTriggers) door.autoClose = 0f; // the trigger closes it once the doorway is empty
                    EditorUtility.SetDirty(door);
                    BuildFrame("DoorFrame_" + door.name, door.transform.parent, b, frameMat, trimMat, cfg, door); doors++;
                }
                foreach (var lintel in Object.FindObjectsOfType<Renderer>().Where(r => r.name.EndsWith("_Lintel")))
                {
                    var lb = lintel.bounds;
                    var opening = new Bounds(new Vector3(lb.center.x, lb.min.y / 2f, lb.center.z), new Vector3(lb.size.x, lb.min.y, lb.size.z));
                    if (doorBounds.Any(d => Vector2.Distance(new Vector2(d.center.x, d.center.z), new Vector2(opening.center.x, opening.center.z)) < 1.2f)) continue;
                    BuildFrame("DoorFrame_" + lintel.name, lintel.transform.parent, opening, frameMat, trimMat, cfg, null); doors++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonLighting] Lit {rooms} room(s), {charLights} character light(s), framed {doors} doorway(s), removed {removed} smoke object(s). Tweak {SettingsPath} and run Dungeon > Improve Lighting & Rooms again.");
        }

        /// Frame (two posts + header) with glowing trims on both faces, a small light and a doorway trigger.
        static void BuildFrame(string name, Transform parent, Bounds open, Material frameMat, Material trimMat, DungeonLightingConfig cfg, BathroomDoor door)
        {
            bool widthOnX = open.size.x >= open.size.z;
            float width = widthOnX ? open.size.x : open.size.z;
            float depth = Mathf.Max(0.3f, widthOnX ? open.size.z : open.size.x) + 0.1f;
            float h = open.max.y, y0 = Mathf.Min(0f, open.min.y), post = 0.18f, trim = 0.04f;
            Vector3 c = new Vector3(open.center.x, 0f, open.center.z);
            Vector3 across = widthOnX ? Vector3.right : Vector3.forward, through = widthOnX ? Vector3.forward : Vector3.right;
            Vector3 Size(float a, float y, float t) => widthOnX ? new Vector3(a, y, t) : new Vector3(t, y, a);

            var root = new GameObject(name).transform; root.SetParent(parent, true); root.position = c;
            var trims = new List<Renderer>();
            foreach (var s in new[] { -1f, 1f })
                Part("Post", root, c + across * s * (width / 2f + post / 2f) + Vector3.up * (h + y0) / 2f, Size(post, h - y0, depth), frameMat);
            Part("Header", root, c + Vector3.up * (h + post / 2f), Size(width + post * 2f, post, depth), frameMat);
            foreach (var face in new[] { -1f, 1f })
            {
                var off = through * face * (depth / 2f + trim / 2f);
                foreach (var s in new[] { -1f, 1f })
                    trims.Add(Part("Trim", root, c + off + across * s * (width / 2f + 0.02f) + Vector3.up * (h + y0) / 2f, Size(trim, h - y0, trim), trimMat));
                trims.Add(Part("Trim", root, c + off + Vector3.up * (h + 0.02f), Size(width + 0.08f, trim, trim), trimMat));
            }
            Light light = null;
            if (cfg.doorLightIntensity > 0f)
            {
                var lg = new GameObject("DoorLight"); lg.transform.SetParent(root, true); lg.transform.position = c + Vector3.up * (h - 0.25f);
                light = lg.AddComponent<Light>(); light.type = LightType.Point; light.range = Mathf.Max(3f, width * 1.6f); light.intensity = cfg.doorLightIntensity; light.shadows = LightShadows.None;
            }
            var tg = new GameObject("DoorwayTrigger"); tg.transform.SetParent(root, true); tg.transform.position = c + Vector3.up * (h + y0) / 2f;
            var box = tg.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = Size(width, h - y0, depth + 2.5f);
            var trig = tg.AddComponent<DoorwayTrigger>();
            trig.door = cfg.doorwayTriggers ? door : null; trig.autoCloseSeconds = cfg.doorAutoClose;
            trig.trims = trims.ToArray(); trig.trimLight = light; trig.closedColor = door ? cfg.doorTrimClosed : cfg.doorTrimOpen; trig.openColor = cfg.doorTrimOpen;
        }

        static Renderer Part(string name, Transform parent, Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true); go.transform.position = pos; go.transform.localScale = size;
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; return r;
        }

        static int RemoveSmoke()
        {
            int n = 0;
            foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true).ToList())
            {
                if (!ps) continue;
                var nm = ps.gameObject.name.ToLowerInvariant();
                if (nm.Contains("smoke") || nm.Contains("fog") || nm.Contains("haze")) { Object.DestroyImmediate(ps.gameObject); n++; }
            }
            foreach (var go in Object.FindObjectsOfType<GameObject>(true)) n += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            return n;
        }

        static string Safe(string s) => string.IsNullOrEmpty(s) ? "Room" : new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        static Material EmissiveAsset(string name, Color emission)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m || m.shader == null || m.shader.name == "Hidden/InternalErrorShader") { if (m) AssetDatabase.DeleteAsset(p); m = LitMaterials.Emissive(name, emission); AssetDatabase.CreateAsset(m, p); }
            else { m.shader = LitMaterials.LitShader(); LitMaterials.SetEmissive(m, emission); }
            EditorUtility.SetDirty(m); return m;
        }

        static Material SolidAsset(string name, Color c, float smooth, float metal)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = LitMaterials.Solid(name, c, smooth, metal); AssetDatabase.CreateAsset(m, p); }
            return m;
        }

        /// Switches every project material still on a built-in shader (Standard etc.) to URP/Lit, keeping colour and texture.
        [MenuItem("Dungeon/Fix Magenta Materials (convert to URP Lit)")]
        public static void ConvertMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!lit) { Debug.LogWarning("[DungeonLighting] URP is not installed."); return; }
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m || !m.shader) continue;
                var sn = m.shader.name;
                if (sn == "Standard" || sn == "Hidden/InternalErrorShader" || sn.StartsWith("Legacy Shaders/"))
                {
                    var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                    var col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    m.shader = lit;
                    if (tex) m.SetTexture("_BaseMap", tex);
                    m.SetColor("_BaseColor", col);
                    EditorUtility.SetDirty(m); n++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonLighting] Converted {n} material(s) to URP Lit.");
        }
    }
}
#endif

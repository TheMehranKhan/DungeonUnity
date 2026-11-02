#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Builds Assets/Scenes/Welcome.unity and Assets/Scenes/Dungeon.unity from the downloaded assets.
/// Menu: Dungeon/Build Scene. Command line: -executeMethod DungeonSetup.BuildScene
public static class DungeonSetup
{
    const string ScenesDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials/Generated";
    const string Models = "Assets/Models/";
    const string Psy = "Assets/Models/Psychedelic/";
    const string Tex = "Assets/Textures/Psychedelic/";

    [MenuItem("Dungeon/Build Scene")]
    public static void BuildScene()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(ScenesDir);
        Directory.CreateDirectory(MatDir);
        string welcome = ScenesDir + "/Welcome.unity";
        string dungeon = ScenesDir + "/Dungeon.unity";
        BuildWelcome(welcome);
        BuildDungeon(dungeon);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(welcome, true), new EditorBuildSettingsScene(dungeon, true) };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(dungeon);
        Debug.Log("[DungeonSetup] Done. Open Assets/Scenes/Welcome.unity and press Play.");
    }

    // ---------- Welcome ----------
    static void BuildWelcome(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.04f, 0.01f, 0.06f);
        camGo.AddComponent<AudioListener>();
        var ui = MakeCanvas("WelcomeCanvas", out var panel, out var button, out var group);
        var ws = ui.AddComponent<WelcomeScreen>(); ws.welcomePanel = panel; ws.playButton = button; ws.fadeGroup = group;
        var loader = ui.AddComponent<LoadSceneButton>(); loader.sceneName = "Dungeon";
        UnityEventTools.AddPersistentListener(button.onClick, loader.Load);
        EditorSceneManager.SaveScene(scene, path);
    }

    static GameObject MakeCanvas(string name, out GameObject panel, out Button button, out CanvasGroup group)
    {
        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        if (Object.FindObjectOfType<EventSystem>() == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        panel = new GameObject("WelcomePanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = new Color(0.05f, 0f, 0.08f, 0.9f);
        group = panel.AddComponent<CanvasGroup>();
        var font = Resources.GetBuiltinResource<Font>(Application.unityVersion.StartsWith("2022") || Application.unityVersion.StartsWith("6") ? "LegacyRuntime.ttf" : "Arial.ttf");

        var title = new GameObject("Title", typeof(RectTransform)); title.transform.SetParent(panel.transform, false);
        var trt = title.GetComponent<RectTransform>(); trt.anchoredPosition = new Vector2(0, 150); trt.sizeDelta = new Vector2(1400, 200);
        var tt = title.AddComponent<Text>(); tt.text = "DUNGEON RAVE"; tt.fontSize = 120; tt.alignment = TextAnchor.MiddleCenter; tt.color = Color.magenta; tt.font = font;

        var btnGo = new GameObject("PlayerModeButton", typeof(RectTransform)); btnGo.transform.SetParent(panel.transform, false);
        var brt = btnGo.GetComponent<RectTransform>(); brt.anchoredPosition = new Vector2(0, -120); brt.sizeDelta = new Vector2(520, 120);
        btnGo.AddComponent<Image>().color = new Color(1f, 0.1f, 0.4f);
        button = btnGo.AddComponent<Button>();
        var lbl = new GameObject("Label", typeof(RectTransform)); lbl.transform.SetParent(btnGo.transform, false);
        Stretch(lbl.GetComponent<RectTransform>());
        var lt = lbl.AddComponent<Text>(); lt.text = "PLAYER MODE"; lt.fontSize = 56; lt.alignment = TextAnchor.MiddleCenter; lt.color = Color.white; lt.font = font;
        return canvasGo;
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    // ---------- Dungeon ----------
    static void BuildDungeon(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Dungeon").transform;

        // Room: 30 x 20, 6 high
        var floorMat = Mat("NeonCrackFloor", new Vector2(6, 4));
        var wallMat = Mat("MeltingRainbowWall", new Vector2(4, 1));
        var ceilMat = Mat("SpiralPortalPattern", new Vector2(3, 2));
        Box("Floor", root, new Vector3(0, -0.25f, 0), new Vector3(30, 0.5f, 20), floorMat);
        Box("Ceiling", root, new Vector3(0, 6.25f, 0), new Vector3(30, 0.5f, 20), ceilMat);
        Box("Wall_N", root, new Vector3(0, 3, 10.25f), new Vector3(30, 6, 0.5f), wallMat);
        Box("Wall_S", root, new Vector3(0, 3, -10.25f), new Vector3(30, 6, 0.5f), wallMat);
        Box("Wall_E", root, new Vector3(15.25f, 3, 0), new Vector3(0.5f, 6, 20), wallMat);
        Box("Wall_W", root, new Vector3(-15.25f, 3, 0), new Vector3(0.5f, 6, 20), wallMat);
        // Bathroom partition in the west corner
        Box("Bathroom_Wall", root, new Vector3(-10, 3, 5), new Vector3(0.3f, 6, 10), wallMat);

        var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "TrippySkybox.png");
        if (skyTex) { var sky = new Material(Shader.Find("Skybox/Panoramic") ?? Shader.Find("Skybox/Procedural")); sky.SetTexture("_MainTex", skyTex); AssetDatabase.CreateAsset(sky, MatDir + "/TrippySkybox.mat"); RenderSettings.skybox = sky; }

        // Stage
        var dj = Place(Models + "dj_booth.glb", root, new Vector3(0, 0, 8), 180, 2.5f);
        var stageTarget = dj ? dj.transform : new GameObject("StageTarget").transform;
        stageTarget.position = new Vector3(0, 1, 8);
        Place(Models + "speaker_stack.glb", root, new Vector3(-5, 0, 8.5f), 160, 3.5f);
        Place(Models + "speaker_stack.glb", root, new Vector3(5, 0, 8.5f), 200, 3.5f);
        Place(Psy + "DJGuy.glb", root, new Vector3(0, 0, 9), 180, 1.8f);

        // Lighting rig
        var raveLights = new List<Light>();
        for (int i = 0; i < 6; i++)
        {
            float x = -10 + i * 4;
            raveLights.Add(SpotLight("RaveSpot" + i, root, new Vector3(x, 5.8f, 0), new Vector3(90, 0, 0), Color.magenta, 4f));
        }
        var raveGo = new GameObject("DungeonRaveLighting"); raveGo.transform.SetParent(root);
        var rave = raveGo.AddComponent<DungeonRaveLighting>(); rave.raveLights = raveLights.ToArray();

        // Lasers
        var lasers = new List<LaserShowController>();
        Vector3[] laserPos = { new Vector3(-3, 5.5f, 9), new Vector3(3, 5.5f, 9) };
        Color[] laserCol = { Color.red, Color.green };
        for (int i = 0; i < laserPos.Length; i++)
        {
            var proj = Place(Models + "laser_projector.glb", root, laserPos[i], 180, 0.6f);
            var emitter = new GameObject("LaserEmitter" + i); emitter.transform.SetParent(root);
            emitter.transform.position = laserPos[i] + Vector3.down * 0.3f;
            emitter.transform.rotation = Quaternion.Euler(15, 180, 0);
            var lsc = emitter.AddComponent<LaserShowController>(); lsc.color = laserCol[i]; lsc.beamCount = 10;
            lasers.Add(lsc);
        }
        Place(Models + "moving_head_camera_rig.glb", root, new Vector3(0, 5.2f, 4), 180, 1.2f);

        // DJ interaction trigger
        var djTrig = new GameObject("DJBoothInteraction"); djTrig.transform.SetParent(root); djTrig.transform.position = new Vector3(0, 1, 6.5f);
        var box = djTrig.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(4, 2, 2);
        djTrig.AddComponent<AudioSource>();
        var dji = djTrig.AddComponent<DJBoothInteraction>(); dji.lasers = lasers.ToArray(); dji.raveLighting = rave;

        // Red lights on walls
        var reds = new List<Light>(); var redRenderers = new List<Renderer>();
        Vector3[] redPos = { new Vector3(-14.7f, 4, -5), new Vector3(14.7f, 4, -5), new Vector3(-14.7f, 4, 5), new Vector3(14.7f, 4, 5) };
        foreach (var p in redPos)
        {
            var fixture = Place(Models + "red_industrial_wall_light.glb", root, p, p.x < 0 ? 90 : -90, 0.6f);
            if (fixture) redRenderers.AddRange(fixture.GetComponentsInChildren<Renderer>());
            reds.Add(PointLight("RedLight", root, p + new Vector3(p.x < 0 ? 0.5f : -0.5f, 0, 0), Color.red, 3f, 8f));
        }
        var redGo = new GameObject("RedLightStrobe"); redGo.transform.SetParent(root);
        var rls = redGo.AddComponent<RedLightStrobe>(); rls.lights = reds.ToArray(); rls.emissiveRenderers = redRenderers.ToArray();

        // Bathroom
        Place(Models + "grimy_toilet.glb", root, new Vector3(-13.5f, 0, 8), 90, 1.0f);
        Place(Models + "sink_with_mirror.glb", root, new Vector3(-13.5f, 0, 3), 90, 1.6f);
        var bathLight = PointLight("BathroomLight", root, new Vector3(-12.5f, 5, 5), new Color(0.75f, 1f, 0.85f), 1.2f, 10f);
        var bathGo = new GameObject("BathroomLights"); bathGo.transform.SetParent(root);
        var bl = bathGo.AddComponent<BathroomLights>(); bl.lights = new[] { bathLight };

        // Props
        Place(Models + "metal_crate_barrier.glb", root, new Vector3(-4, 0, 5), 0, 1.2f);
        Place(Models + "metal_crate_barrier.glb", root, new Vector3(4, 0, 5), 0, 1.2f);
        var props = new (string, Vector3, float)[] {
            ("Bouncer", new Vector3(13, 0, -8), 1.9f), ("RaverGirl", new Vector3(-2, 0, 1), 1.7f), ("CrystalTotem", new Vector3(12, 0, 8), 2f),
            ("FloatingEyeball", new Vector3(0, 4, 0), 1f), ("HeartPill", new Vector3(6, 0.5f, -3), 0.6f), ("SmileyPill", new Vector3(-6, 0.5f, -3), 0.6f),
            ("RainbowCapsule", new Vector3(8, 0.5f, 2), 0.6f), ("LavaLamp", new Vector3(-8, 0, -8), 1.2f), ("MeltingMushroom", new Vector3(10, 0, -2), 1.5f),
            ("WarpedDiscoBall", new Vector3(0, 5, 2), 1.2f) };
        foreach (var (n, p, s) in props) Place(Psy + n + ".glb", root, p, 0, s);

        // Player
        var player = new GameObject("Player"); player.tag = "Player"; player.transform.position = new Vector3(0, 1.1f, -7);
        var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f;
        var pivot = new GameObject("CameraPivot"); pivot.transform.SetParent(player.transform, false); pivot.transform.localPosition = new Vector3(0, 0.7f, 0);
        var pcam = pivot.AddComponent<Camera>(); pivot.AddComponent<AudioListener>(); pivot.tag = "MainCamera";
        var pmc = player.AddComponent<PlayerModeController>(); pmc.cameraPivot = pivot.transform;

        // Spectator camera (CameraDirector) — moves the player camera until player mode starts
        var shotsRoot = new GameObject("CameraShots"); shotsRoot.transform.SetParent(root);
        Vector3[] shotPos = { new Vector3(0, 3, -8), new Vector3(-10, 4, 0), new Vector3(10, 2, 4), new Vector3(0, 5, 2) };
        var shots = new List<Transform>();
        for (int i = 0; i < shotPos.Length; i++) { var s = new GameObject("Shot" + i).transform; s.SetParent(shotsRoot.transform); s.position = shotPos[i]; s.LookAt(stageTarget.position); shots.Add(s); }
        var dirGo = new GameObject("CinematicCamera"); dirGo.transform.position = shotPos[0];
        var dcam = dirGo.AddComponent<Camera>(); dcam.depth = 1;
        var director = dirGo.AddComponent<CameraDirector>(); director.cam = dcam; director.shots = shots.ToArray(); director.lookTarget = stageTarget;

        // Welcome overlay inside the dungeon (cinematic cam until Player Mode)
        var ui = MakeCanvas("WelcomeCanvas", out var panel, out var button, out var group);
        var ws = ui.AddComponent<WelcomeScreen>(); ws.welcomePanel = panel; ws.playButton = button; ws.fadeGroup = group; ws.playerMode = pmc;
        pmc.welcomePanel = panel;
        UnityEventTools.AddBoolPersistentListener(button.onClick, dirGo.SetActive, false);

        EditorSceneManager.SaveScene(scene, path);
    }

    // ---------- helpers ----------
    static GameObject Place(string assetPath, Transform parent, Vector3 pos, float yaw, float targetHeight)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!prefab) { Debug.LogWarning("[DungeonSetup] Missing model, skipped: " + assetPath + " (run setup.command to download it)"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        // normalise size, then sit it on the floor
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.0001f) go.transform.localScale *= targetHeight / b.size.y;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.position = pos + new Vector3(0, -b.min.y + go.transform.position.y, 0);
            if (pos.y > 0.01f) go.transform.position = pos;
        }
        else go.transform.position = pos;
        return go;
    }

    static Material Mat(string texName, Vector2 tiling)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { name = texName };
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + texName + ".png");
        if (tex)
        {
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
            if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tiling); }
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white * 0.35f);
        }
        else Debug.LogWarning("[DungeonSetup] Missing texture, using plain material: " + texName);
        string p = MatDir + "/" + texName + ".mat";
        AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = mat; return go;
    }

    static Light SpotLight(string name, Transform parent, Vector3 pos, Vector3 euler, Color c, float intensity)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = pos; go.transform.eulerAngles = euler;
        var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.color = c; l.intensity = intensity; l.range = 12; l.spotAngle = 50; return l;
    }

    static Light PointLight(string name, Transform parent, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = pos;
        var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; return l;
    }
}
#endif

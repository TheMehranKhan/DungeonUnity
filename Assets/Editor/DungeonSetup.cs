#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonUnity.Editor
{
    /// Builds Assets/Scenes/Welcome.unity and Assets/Scenes/Dungeon.unity with everything wired up.
    /// Safe to run again: scenes are rebuilt, existing configs and materials are reused, missing models are skipped.
    /// Menu: Dungeon/Build Scene. Command line: -executeMethod DungeonUnity.Editor.DungeonSetup.BuildScene
    public static class DungeonSetup
    {
        const string ScenesDir = "Assets/Scenes";
        const string MatDir = "Assets/Materials/Generated";
        const string SettingsDir = "Assets/Settings";
        const string AudioDir = "Assets/Audio";
        const string Models = "Assets/Models/";
        const string Psy = "Assets/Models/Psychedelic/";
        const string Tex = "Assets/Textures/Psychedelic/";

        static Font font;
        static DungeonConfig config;
        static CharacterRigConfig rigConfig;
        static readonly List<string> warnings = new List<string>();

        [MenuItem("Dungeon/Build Scene")]
        public static void BuildScene()
        {
            warnings.Clear();
            AssetDatabase.Refresh();
            foreach (var d in new[] { ScenesDir, MatDir, SettingsDir }) Directory.CreateDirectory(d);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            config = LoadOrCreate<DungeonConfig>(SettingsDir + "/DungeonConfig.asset");
            rigConfig = LoadOrCreate<CharacterRigConfig>(SettingsDir + "/CharacterRigConfig.asset");

            string welcome = ScenesDir + "/Welcome.unity", dungeon = ScenesDir + "/Dungeon.unity";
            BuildWelcome(welcome);
            BuildDungeon(dungeon);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(welcome, true), new EditorBuildSettingsScene(dungeon, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(welcome);
            foreach (var w in warnings) Debug.LogWarning("[DungeonSetup] " + w);
            Debug.Log($"[DungeonSetup] Done with {warnings.Count} warning(s). Open Assets/Scenes/Welcome.unity and press Play.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); return a;
        }

        // ---------- audio ----------
        static AudioClip Clip(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");
            if (!c) warnings.Add("Missing audio " + name + ".wav");
            return c;
        }

        static AudioManager MakeAudio(Transform parent, bool withMusic)
        {
            var go = new GameObject("AudioManager"); go.transform.SetParent(parent);
            var am = go.AddComponent<AudioManager>();
            am.config = config;
            am.music = withMusic ? Clip("Music_Techno128_Loop") : null;
            am.ambience = withMusic ? Clip("Amb_Crowd") : Clip("Amb_RoomTone");
            am.playMusicOnStart = withMusic;
            AudioManager.NamedClip N(string n, float vol, float jitter, params string[] files)
                => new AudioManager.NamedClip { name = n, variants = files.Select(Clip).Where(c => c).ToArray(), volume = vol, pitchJitter = jitter };
            am.sfx = new[] {
                N("LaserZap", 0.5f, 0.1f, "SFX_LaserZap"), N("DoorCreak", 0.9f, 0.05f, "SFX_DoorCreak"),
                N("PillPickup", 0.9f, 0f, "SFX_PillPickup"), N("ToiletFlush", 0.9f, 0.03f, "SFX_ToiletFlush"),
                N("BassDrop", 1f, 0f, "SFX_BassDrop"), N("UIClick", 0.7f, 0.05f, "UI_Click"), N("UIHover", 0.4f, 0.05f, "UI_Hover"),
                N("Footstep", 0.6f, 0.08f, "SFX_Footstep_1", "SFX_Footstep_2", "SFX_Footstep_3", "SFX_Footstep_4") };
            var beat = go.AddComponent<MusicBeatSync>(); beat.bpm = config.bpm;
            return am;
        }

        // ---------- Welcome ----------
        static void BuildWelcome(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.04f, 0.01f, 0.06f);
            camGo.AddComponent<AudioListener>();
            MakeAudio(null, false);
            new GameObject("SceneFader").AddComponent<SceneFader>();

            var canvas = MakeCanvas("WelcomeCanvas", 10);
            var panel = Panel("WelcomePanel", canvas.transform, new Color(0.05f, 0f, 0.08f, 0.92f));
            var title = Label("Title", panel.transform, "DUNGEON RAVE", 120, new Vector2(0, 220), new Vector2(1400, 200));
            Label("Subtitle", panel.transform, "WASD move  -  Mouse look  -  E interact  -  Esc pause", 34, new Vector2(0, 90), new Vector2(1400, 60)).color = new Color(1, 1, 1, 0.7f);
            var enter = MakeButton("EnterButton", panel.transform, "ENTER THE DUNGEON", new Vector2(0, -60));
            var quit = MakeButton("QuitButton", panel.transform, "QUIT", new Vector2(0, -210));
            var ws = canvas.AddComponent<WelcomeScreen>(); ws.welcomePanel = panel; ws.title = title; ws.startOnEnter = false;
            var loader = canvas.AddComponent<LoadSceneButton>(); loader.sceneName = "Dungeon";
            UnityEventTools.AddPersistentListener(enter.onClick, loader.Load);
            UnityEventTools.AddPersistentListener(quit.onClick, loader.Quit);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ---------- Dungeon ----------
        static void BuildDungeon(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Dungeon").transform;
            var systems = new GameObject("Systems").transform;
            var audio = MakeAudio(systems, true);
            new GameObject("SceneFader").AddComponent<SceneFader>().transform.SetParent(systems);

            // Room 30 x 20 x 6
            var floorMat = Mat("NeonCrackFloor", new Vector2(6, 4));
            var wallMat = Mat("MeltingRainbowWall", new Vector2(4, 1));
            var ceilMat = Mat("SpiralPortalPattern", new Vector2(3, 2));
            Box("Floor", root, new Vector3(0, -0.25f, 0), new Vector3(30, 0.5f, 20), floorMat);
            Box("Ceiling", root, new Vector3(0, 6.25f, 0), new Vector3(30, 0.5f, 20), ceilMat);
            Box("Wall_N", root, new Vector3(0, 3, 10.25f), new Vector3(30, 6, 0.5f), wallMat);
            Box("Wall_S", root, new Vector3(0, 3, -10.25f), new Vector3(30, 6, 0.5f), wallMat);
            Box("Wall_E", root, new Vector3(15.25f, 3, 0), new Vector3(0.5f, 6, 20), wallMat);
            Box("Wall_W", root, new Vector3(-15.25f, 3, 0), new Vector3(0.5f, 6, 20), wallMat);
            // Bathroom: partition with a doorway (z 0..2) and a hinged door
            Box("Bathroom_Wall_A", root, new Vector3(-10, 3, 6), new Vector3(0.3f, 6, 8), wallMat);
            Box("Bathroom_Wall_B", root, new Vector3(-10, 4.5f, 1), new Vector3(0.3f, 3, 2), wallMat);
            Box("Bathroom_Wall_C", root, new Vector3(-12.6f, 3, 0), new Vector3(5f, 6, 0.3f), wallMat);

            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "TrippySkybox.png");
            if (skyTex)
            {
                string sp = MatDir + "/TrippySkybox.mat";
                var sky = AssetDatabase.LoadAssetAtPath<Material>(sp);
                if (!sky) { var sh = Shader.Find("Skybox/Panoramic") ?? Shader.Find("Skybox/Procedural"); sky = new Material(sh); AssetDatabase.CreateAsset(sky, sp); }
                sky.SetTexture("_MainTex", skyTex); RenderSettings.skybox = sky;
            }

            // Stage
            var dj = Place(Models + "dj_booth.glb", root, new Vector3(0, 0, 8), 180, 2.5f);
            var stageTarget = new GameObject("StageTarget").transform; stageTarget.SetParent(root); stageTarget.position = new Vector3(0, 1.2f, 8);
            foreach (var x in new[] { -5f, 5f })
            {
                var spk = Place(Models + "speaker_stack.glb", root, new Vector3(x, 0, 8.5f), x < 0 ? 160 : 200, 3.5f);
                if (spk) spk.AddComponent<SpeakerPulse>().squashAndStretch = true;
            }
            var djGuy = Place(Psy + "DJGuy.glb", root, new Vector3(0, 0, 9), 180, 1.8f);
            if (djGuy) { var d = djGuy.AddComponent<CrowdDancer>(); d.style = CrowdDancer.DanceStyle.Headbang; d.bpm = config.bpm; }

            // Rave lights
            var raveLights = new List<Light>();
            for (int i = 0; i < 6; i++) raveLights.Add(SpotLight("RaveSpot" + i, root, new Vector3(-10 + i * 4, 5.8f, 0), new Vector3(90, 0, 0), Color.magenta, 4f));
            var rave = new GameObject("DungeonRaveLighting").AddComponent<DungeonRaveLighting>();
            rave.transform.SetParent(systems); rave.raveLights = raveLights.ToArray(); rave.config = config; rave.bpm = config.bpm;

            // Lasers
            var laserRoot = new GameObject("Lasers").transform; laserRoot.SetParent(root);
            Vector3[] laserPos = { new Vector3(-3, 5.5f, 9), new Vector3(3, 5.5f, 9) };
            Color[] laserCol = { Color.red, Color.green };
            for (int i = 0; i < laserPos.Length; i++)
            {
                Place(Models + "laser_projector.glb", root, laserPos[i], 180, 0.6f);
                var em = new GameObject("LaserEmitter" + i); em.transform.SetParent(laserRoot);
                em.transform.position = laserPos[i] + Vector3.down * 0.3f; em.transform.rotation = Quaternion.Euler(15, 180, 0);
                var l = em.AddComponent<LaserShowController>(); l.color = laserCol[i]; l.beamCount = 10; l.bpm = config.bpm;
                if (i > 0) l.patternChangeSfx = "";
            }
            laserRoot.gameObject.SetActive(config.enableLasers);
            Place(Models + "moving_head_camera_rig.glb", root, new Vector3(0, 5.2f, 4), 180, 1.2f);

            // Disco ball
            var ballGo = Place(Psy + "WarpedDiscoBall.glb", root, new Vector3(0, 5, 2), 0, 1.2f);
            if (!ballGo) { ballGo = new GameObject("DiscoBallPivot"); ballGo.transform.SetParent(root); ballGo.transform.position = new Vector3(0, 5, 2); }
            ballGo.AddComponent<DiscoBallSpin>();

            // DJ booth interaction (toggles music + lasers)
            var djTrig = new GameObject("DJBoothInteraction"); djTrig.transform.SetParent(root); djTrig.transform.position = new Vector3(0, 1.2f, 6.8f);
            var box = djTrig.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(4, 2, 1.5f);
            var dji = djTrig.AddComponent<DJBoothInteraction>();
            dji.prompt = "Press {key} to {state} the music"; dji.sfx = "UIClick";
            dji.raveLighting = rave; dji.lasers = laserRoot.gameObject; dji.toggleLasers = true; dji.dropSfx = "BassDrop";

            // Red wall lights
            var reds = new List<Light>(); var redRenderers = new List<Renderer>();
            foreach (var p in new[] { new Vector3(-14.7f, 4, -5), new Vector3(14.7f, 4, -5), new Vector3(-14.7f, 4, 5), new Vector3(14.7f, 4, 5) })
            {
                var fixture = Place(Models + "red_industrial_wall_light.glb", root, p, p.x < 0 ? 90 : -90, 0.6f);
                if (fixture) redRenderers.AddRange(fixture.GetComponentsInChildren<Renderer>());
                reds.Add(PointLight("RedLight", root, p + new Vector3(p.x < 0 ? 0.5f : -0.5f, 0, 0), Color.red, 3f, 8f));
            }
            var rls = new GameObject("RedLightStrobe").AddComponent<RedLightStrobe>(); rls.transform.SetParent(systems);
            rls.lights = reds.ToArray(); rls.emissiveRenderers = redRenderers.ToArray(); rls.bpm = config.bpm;

            // Bathroom
            var toilet = Place(Models + "grimy_toilet.glb", root, new Vector3(-13.5f, 0, 8), 90, 1.0f);
            if (toilet) Interact<SoundInteractable>(toilet, "Press {key} to flush", "ToiletFlush").cooldown = 4f;
            Place(Models + "sink_with_mirror.glb", root, new Vector3(-13.5f, 0, 3), 90, 1.6f);
            var bathLight = PointLight("BathroomLight", root, new Vector3(-12.5f, 5, 5), new Color(0.75f, 1f, 0.85f), 1.2f, 10f);
            var bl = new GameObject("BathroomLights").AddComponent<BathroomLights>(); bl.transform.SetParent(systems); bl.lights = new[] { bathLight };
            var buzz = bl.gameObject.AddComponent<AudioSource>(); buzz.clip = Clip("Amb_RoomTone"); buzz.loop = true; buzz.playOnAwake = true; buzz.spatialBlend = 1f; bl.buzz = buzz;
            bl.transform.position = new Vector3(-12.5f, 3, 5);
            var zone = new GameObject("BathroomZone"); zone.transform.SetParent(root); zone.transform.position = new Vector3(-12.6f, 3, 5);
            var zb = zone.AddComponent<BoxCollider>(); zb.isTrigger = true; zb.size = new Vector3(5, 6, 10);
            var rz = zone.AddComponent<RoomLightingZone>(); rz.config = config; rz.roomName = "Bathroom"; rz.exitRoomName = "DanceFloor"; rz.lighting = rave; rz.lasers = laserRoot.GetComponentsInChildren<LaserShowController>(true);
            // Door in the doorway (hinge at z=0)
            var hinge = new GameObject("BathroomDoorHinge"); hinge.transform.SetParent(root); hinge.transform.position = new Vector3(-10, 0, 0.05f);
            var door = Box("BathroomDoor", hinge.transform, new Vector3(-10, 1.5f, 1), new Vector3(0.1f, 3, 1.9f), wallMat);
            var bd = hinge.AddComponent<BoxCollider>(); bd.center = new Vector3(0, 1.5f, 0.95f); bd.size = new Vector3(0.2f, 3, 1.9f);
            Object.DestroyImmediate(door.GetComponent<Collider>());
            var bdoor = hinge.AddComponent<BathroomDoor>(); bdoor.hinge = hinge.transform; bdoor.prompt = "Press {key} to open/close the door"; bdoor.sfx = "DoorCreak";

            // Props + pills
            Place(Models + "metal_crate_barrier.glb", root, new Vector3(-4, 0, 5), 0, 1.2f);
            Place(Models + "metal_crate_barrier.glb", root, new Vector3(4, 0, 5), 0, 1.2f);
            var props = new (string, Vector3, float)[] {
                ("CrystalTotem", new Vector3(12, 0, 8), 2f), ("FloatingEyeball", new Vector3(0, 4, 0), 1f),
                ("LavaLamp", new Vector3(-8, 0, -8), 1.2f), ("MeltingMushroom", new Vector3(10, 0, -2), 1.5f) };
            foreach (var (n, p, s) in props) Place(Psy + n + ".glb", root, p, 0, s);
            if (config.enableTrippyPills)
                foreach (var (n, p) in new[] { ("HeartPill", new Vector3(6, 1f, -3)), ("SmileyPill", new Vector3(-6, 1f, -3)), ("RainbowCapsule", new Vector3(-12, 1f, 6)) })
                {
                    var pill = Place(Psy + n + ".glb", root, p, 0, 0.4f);
                    if (!pill) { pill = GameObject.CreatePrimitive(PrimitiveType.Capsule); pill.name = n; pill.transform.SetParent(root); pill.transform.position = p; pill.transform.localScale = Vector3.one * 0.25f; }
                    var sc = pill.GetComponent<Collider>(); if (!sc) { var s = pill.AddComponent<SphereCollider>(); s.radius = 0.5f; }
                    Interact<PillPickup>(pill, "Press {key} to take the pill", "PillPickup");
                }

            // Crowd
            var crowdRoot = new GameObject("Crowd").transform; crowdRoot.SetParent(root);
            var people = new[] { "RaverGirl", "Bouncer" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(Psy + n + ".glb")).Where(g => g).ToArray();
            var bouncer = Place(Psy + "Bouncer.glb", crowdRoot, new Vector3(13, 0, -8), -45, 1.9f);
            if (bouncer) { var d = bouncer.AddComponent<CrowdDancer>(); d.style = CrowdDancer.DanceStyle.Bob; d.bobHeight = 0.02f; }
            if (config.enableCrowd)
            {
                var rng = new System.Random(1234);
                int count = Mathf.Max(1, config.crowdCount);
                for (int i = 0; i < count; i++)
                {
                    var pos = new Vector3((float)(rng.NextDouble() * 2 - 1) * config.crowdArea.x, 0, 1.5f + (float)(rng.NextDouble() * 2 - 1) * config.crowdArea.y * 0.5f);
                    GameObject m = people.Length > 0 ? Place(AssetDatabase.GetAssetPath(people[i % people.Length]), crowdRoot, pos, 180 + rng.Next(-40, 40), 1.6f + (float)rng.NextDouble() * 0.3f) : null;
                    if (!m) { m = GameObject.CreatePrimitive(PrimitiveType.Capsule); m.name = "Dancer" + i; m.transform.SetParent(crowdRoot); m.transform.position = pos + Vector3.up; }
                    m.AddComponent<CrowdDancer>().bpm = config.bpm;
                }
            }

            // Wandering NPC with procedural legs (CharacterRig + ProceduralWalk + AutoPilot)
            {
                var npc = new GameObject("Wanderer"); npc.transform.SetParent(crowdRoot); npc.transform.position = new Vector3(-6, 0.05f, -2);
                var ncc = npc.AddComponent<CharacterController>(); ncc.height = 1.7f; ncc.radius = 0.3f; ncc.center = new Vector3(0, 0.85f, 0);
                var body = people.Length > 0 ? Place(AssetDatabase.GetAssetPath(people[0]), npc.transform, npc.transform.position + Vector3.up * rigConfig.legLength, 0, 0.9f) : null;
                if (!body) { body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.DestroyImmediate(body.GetComponent<Collider>()); body.transform.SetParent(npc.transform, false); body.transform.localPosition = new Vector3(0, rigConfig.legLength + 0.45f, 0); body.transform.localScale = new Vector3(0.5f, 0.45f, 0.35f); }
                body.name = "Body";
                var rig = npc.AddComponent<CharacterRig>(); rig.config = rigConfig;
                npc.AddComponent<ProceduralWalk>().bodyVisual = body.transform;
                var route = new[] { new Vector3(-6, 0, -2), new Vector3(6, 0, -2), new Vector3(6, 0, -6), new Vector3(-6, 0, -6) }
                    .Select((p, i) => { var t = new GameObject("NpcWaypoint" + i).transform; t.SetParent(crowdRoot); t.position = p; return t; }).ToArray();
                var nap = npc.AddComponent<AutoPilot>(); nap.alwaysOn = true; nap.requireIdle = false; nap.waypoints = route; nap.speed = 1.4f; nap.order = AutoPilot.RouteOrder.PingPong; nap.lookAtWhileDwelling = stageTarget;
            }

            // Smoke
            if (config.enableSmoke)
                foreach (var x in new[] { -6f, 6f })
                {
                    var sm = new GameObject("SmokeMachine"); sm.transform.SetParent(root); sm.transform.position = new Vector3(x, 0.2f, 8);
                    sm.AddComponent<ParticleSystem>(); sm.AddComponent<SmokeMachine>();
                }

            // Player
            var player = new GameObject("Player") { tag = "Player" }; player.transform.position = new Vector3(0, 1.1f, -7);
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f;
            var pivot = new GameObject("CameraPivot"); pivot.transform.SetParent(player.transform, false); pivot.transform.localPosition = new Vector3(0, 0.7f, 0);
            var pcam = pivot.AddComponent<Camera>(); pivot.AddComponent<AudioListener>(); pivot.tag = "MainCamera"; pcam.nearClipPlane = 0.05f;
            var pmc = player.AddComponent<PlayerModeController>(); pmc.cameraPivot = pivot.transform; pmc.rigConfig = rigConfig;
            var steps = player.AddComponent<PlayerFootsteps>();
            steps.steps = new[] { 1, 2, 3, 4 }.Select(i => Clip("SFX_Footstep_" + i)).Where(c => c).ToArray();
            var trippy = pivot.AddComponent<TrippyEffect>();
            if (config.enableAutoPilot)
            {
                var wpRoot = new GameObject("AutoPilotRoute").transform; wpRoot.SetParent(root);
                var wps = new[] { new Vector3(0, 0, 4), new Vector3(-6, 0, 0), new Vector3(6, 0, 0), new Vector3(0, 0, -5) }
                    .Select((p, i) => { var t = new GameObject("Waypoint" + i).transform; t.SetParent(wpRoot); t.position = p; return t; }).ToArray();
                var ap = player.AddComponent<AutoPilot>(); ap.waypoints = wps; ap.lookAtWhileDwelling = stageTarget; ap.order = AutoPilot.RouteOrder.Random;
            }

            // Post FX (URP via reflection; harmless without URP)
            var fx = new GameObject("PostFX"); fx.transform.SetParent(systems);
            var post = fx.AddComponent<PostFXSetup>(); post.enabled = config.enablePostFX;
            trippy.postFX = post;

            // Cinematic camera for the welcome overlay
            var shotsRoot = new GameObject("CameraShots"); shotsRoot.transform.SetParent(root);
            var shots = new[] { new Vector3(0, 3, -8), new Vector3(-10, 4, 0), new Vector3(10, 2, 4), new Vector3(0, 5, 2) }
                .Select((p, i) => { var s = new GameObject("Shot" + i).transform; s.SetParent(shotsRoot.transform); s.position = p; s.LookAt(stageTarget.position); return s; }).ToArray();
            var dirGo = new GameObject("CinematicCamera"); dirGo.transform.position = shots[0].position;
            var dcam = dirGo.AddComponent<Camera>(); dcam.depth = 1;
            var director = dirGo.AddComponent<CameraDirector>(); director.cam = dcam; director.shots = shots; director.lookTarget = stageTarget; director.bpm = config.bpm;
            post.cameras = new[] { pcam, dcam };

            // HUD: crosshair + prompt + trippy overlay
            var hud = MakeCanvas("HUD", 5);
            var overlay = Panel("TrippyOverlay", hud.transform, new Color(1, 0, 1, 0)).GetComponent<Image>(); overlay.raycastTarget = false; overlay.enabled = false;
            trippy.overlay = overlay;
            var cross = Label("Crosshair", hud.transform, "+", 48, Vector2.zero, new Vector2(60, 60));
            var promptText = Label("Prompt", hud.transform, "", 36, new Vector2(0, -120), new Vector2(1200, 60));
            var ip = hud.AddComponent<InteractionPrompt>(); ip.cam = pcam; ip.promptText = promptText; ip.crosshair = cross; ip.player = pmc;

            // Welcome overlay
            var wc = MakeCanvas("WelcomeCanvas", 10);
            var panel = Panel("WelcomePanel", wc.transform, new Color(0.05f, 0f, 0.08f, 0.6f));
            var group = panel.AddComponent<CanvasGroup>();
            var title = Label("Title", panel.transform, "DUNGEON RAVE", 120, new Vector2(0, 150), new Vector2(1400, 200));
            var play = MakeButton("PlayerModeButton", panel.transform, "PLAYER MODE", new Vector2(0, -120));
            var ws = wc.AddComponent<WelcomeScreen>(); ws.welcomePanel = panel; ws.playButton = play; ws.fadeGroup = group; ws.playerMode = pmc; ws.title = title;
            ws.disableOnPlay = new[] { dirGo };
            pmc.welcomePanel = panel;

            // Pause + settings
            var pc = MakeCanvas("PauseCanvas", 20);
            var pausePanel = Panel("PausePanel", pc.transform, new Color(0, 0, 0, 0.75f));
            Label("PausedTitle", pausePanel.transform, "PAUSED", 90, new Vector2(0, 250), new Vector2(800, 140));
            var resume = MakeButton("Resume", pausePanel.transform, "RESUME", new Vector2(0, 60));
            var settingsB = MakeButton("Settings", pausePanel.transform, "SETTINGS", new Vector2(0, -80));
            var quitB = MakeButton("Quit", pausePanel.transform, "QUIT TO MENU", new Vector2(0, -220));
            var setPanel = Panel("SettingsPanel", pc.transform, new Color(0.04f, 0, 0.07f, 0.95f));
            Label("SettingsTitle", setPanel.transform, "SETTINGS", 80, new Vector2(0, 360), new Vector2(800, 120));
            var sm2 = pc.AddComponent<SettingsMenu>(); sm2.panel = setPanel;
            sm2.master = MakeSlider(setPanel.transform, "Master", 220, 0, 1);
            sm2.music = MakeSlider(setPanel.transform, "Music", 130, 0, 1);
            sm2.sfx = MakeSlider(setPanel.transform, "SFX", 40, 0, 1);
            sm2.ambience = MakeSlider(setPanel.transform, "Ambience", -50, 0, 1);
            sm2.sensitivity = MakeSlider(setPanel.transform, "Mouse sensitivity", -140, 0.1f, 10);
            sm2.invertY = MakeToggle(setPanel.transform, "Invert Y", -230);
            sm2.backButton = MakeButton("Back", setPanel.transform, "BACK", new Vector2(0, -360));
            var pm = pc.AddComponent<PauseMenu>(); pm.panel = pausePanel; pm.settings = sm2; pm.player = pmc;
            pm.resumeButton = resume; pm.settingsButton = settingsB; pm.quitButton = quitB;

            // Exit door portal near the south wall
            var exit = GameObject.CreatePrimitive(PrimitiveType.Cube); exit.name = "ExitDoor"; exit.transform.SetParent(root);
            exit.transform.position = new Vector3(0, 1.5f, -9.9f); exit.transform.localScale = new Vector3(1.6f, 3, 0.2f);
            Interact<ScenePortal>(exit, "Press {key} to leave the club", "DoorCreak").sceneName = "Welcome";

            EditorSceneManager.SaveScene(scene, path);
        }

        // ---------- helpers ----------
        static T Interact<T>(GameObject go, string prompt, string sfx) where T : Interactable
        {
            if (!go.GetComponent<Collider>()) { var b = go.AddComponent<BoxCollider>(); FitBox(go, b); }
            var c = go.GetComponent<T>(); if (!c) c = go.AddComponent<T>(); c.prompt = prompt; c.sfx = sfx; return c;
        }

        static void FitBox(GameObject go, BoxCollider b)
        {
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
            var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds);
            b.center = go.transform.InverseTransformPoint(bounds.center);
            var s = go.transform.lossyScale; b.size = new Vector3(bounds.size.x / Mathf.Max(1e-4f, s.x), bounds.size.y / Mathf.Max(1e-4f, s.y), bounds.size.z / Mathf.Max(1e-4f, s.z));
        }

        static GameObject MakeCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (!Object.FindObjectOfType<EventSystem>()) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }
            return go;
        }

        static GameObject Panel(string name, Transform parent, Color c)
        {
            var p = new GameObject(name, typeof(RectTransform)); p.transform.SetParent(parent, false);
            Stretch(p.GetComponent<RectTransform>()); p.AddComponent<Image>().color = c; return p;
        }

        static Text Label(string name, Transform parent, string text, int size, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = dim;
            var t = go.AddComponent<Text>(); t.text = text; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.font = font; t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(string name, Transform parent, string text, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(560, 110);
            go.AddComponent<Image>().color = new Color(1f, 0.1f, 0.4f);
            var b = go.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1f, 0.6f, 0.8f); b.colors = cb;
            go.AddComponent<UISound>();
            var l = Label("Label", go.transform, text, 48, Vector2.zero, Vector2.zero); Stretch(l.rectTransform);
            return b;
        }

        static Slider MakeSlider(Transform parent, string label, float y, float min, float max)
        {
            Label(label + "Label", parent, label, 36, new Vector2(-330, y), new Vector2(420, 60)).alignment = TextAnchor.MiddleRight;
            var go = new GameObject(label + "Slider", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(180, y); rt.sizeDelta = new Vector2(520, 30);
            var bg = Panel("Background", go.transform, new Color(1, 1, 1, 0.2f));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false); Stretch(fillArea.GetComponent<RectTransform>());
            var fill = Panel("Fill", fillArea.transform, new Color(1f, 0.1f, 0.4f));
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); handleArea.transform.SetParent(go.transform, false); Stretch(handleArea.GetComponent<RectTransform>());
            var handle = new GameObject("Handle", typeof(RectTransform)); handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 0); var hi = handle.AddComponent<Image>(); hi.color = Color.white;
            var s = go.AddComponent<Slider>(); s.fillRect = fill.GetComponent<RectTransform>(); s.handleRect = handle.GetComponent<RectTransform>(); s.targetGraphic = hi;
            s.minValue = min; s.maxValue = max; s.value = max;
            bg.GetComponent<Image>().raycastTarget = true;
            return s;
        }

        static Toggle MakeToggle(Transform parent, string label, float y)
        {
            Label(label + "Label", parent, label, 36, new Vector2(-330, y), new Vector2(420, 60)).alignment = TextAnchor.MiddleRight;
            var go = new GameObject(label + "Toggle", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(-60, y); rt.sizeDelta = new Vector2(50, 50);
            var bg = go.AddComponent<Image>(); bg.color = new Color(1, 1, 1, 0.25f);
            var check = Panel("Checkmark", go.transform, new Color(1f, 0.1f, 0.4f)); check.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8); check.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);
            var t = go.AddComponent<Toggle>(); t.targetGraphic = bg; t.graphic = check.GetComponent<Image>(); t.isOn = false;
            return t;
        }

        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        static GameObject Place(string assetPath, Transform parent, Vector3 pos, float yaw, float targetHeight)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!prefab) { warnings.Add("Missing model, skipped: " + assetPath + " (run setup.command to download it)"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.0001f) go.transform.localScale *= targetHeight / b.size.y;
                go.transform.position = Vector3.zero;
                b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                go.transform.position = pos.y > 0.01f ? pos : pos + new Vector3(0, -b.min.y, 0);
            }
            else go.transform.position = pos;
            return go;
        }

        static Material Mat(string texName, Vector2 tiling)
        {
            string p = MatDir + "/" + texName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader) { name = texName }; AssetDatabase.CreateAsset(m, p);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + texName + ".png");
            if (tex)
            {
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tiling); }
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white * 0.35f);
            }
            else warnings.Add("Missing texture, using plain material: " + texName);
            EditorUtility.SetDirty(m);
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
}
#endif

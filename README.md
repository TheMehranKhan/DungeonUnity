# DungeonUnity

## Quick start (macOS)

One line in Terminal:

```
curl -fsSL https://raw.githubusercontent.com/TheMehranKhan/DungeonUnity/main/setup.command | zsh
```

Or clone the repo and double-click `setup.command` in Finder.

It clones or updates the project into `~/DungeonUnity`, downloads all models, textures and previews from the [assets-v1 release](https://github.com/TheMehranKhan/DungeonUnity/releases/tag/assets-v1) (resumable, checksum-verified), then opens Unity 2022.3 and runs `DungeonSetup.BuildScene`, which creates `Assets/Scenes/Welcome.unity` and `Assets/Scenes/Dungeon.unity` and adds them to Build Settings. Open `Welcome.unity` and press Play. You can rebuild any time from the menu **Dungeon > Build Scene**. Missing models are skipped with a warning.

Options: `DUNGEON_DIR=/path` to install elsewhere, `SKIP_UNITY=1` to only download.

## Psychedelic pack

Downloaded by `setup.command` (release asset `Psychedelic-pack.zip`):

- `Assets/Models/Psychedelic/`: Bouncer, CrystalTotem, DJGuy, FloatingEyeball, HeartPill, LavaLamp, MeltingMushroom, RainbowCapsule, RaverGirl, SmileyPill, WarpedDiscoBall (.glb)
- `Assets/Textures/Psychedelic/`: MeltingRainbowWall, NeonCrackFloor, SpiralPortalPattern, TrippySkybox, PsychedelicSpriteSheet (.png)
- `previews/`: a PNG preview of each psychedelic model

The big club models (`Assets/Models/*.glb`) are release assets too, since they are about 45 MB each.


A dungeon rave club kit for Unity: lighting, lasers, a DJ booth, big speakers, a grimy bathroom and red lights, plus a welcome screen with a button that drops you into first-person player mode.

## Open it in Unity
1. Install Unity 2022.3 LTS or newer (Unity 6 works too).
2. Clone the repo, then in Unity Hub choose **Add project from disk** and pick the `DungeonUnity` folder. Unity creates `ProjectSettings/` and `Library/` on first open.
3. Unity resolves `Packages/manifest.json`, which includes **glTFast** (`com.unity.cloud.gltfast`). glTFast is what imports the `.glb` models in `Assets/Models/`. If you copy the assets into a different project, add glTFast there through Package Manager > Add package by name > `com.unity.cloud.gltfast`, or add the line to that project's `Packages/manifest.json`.

## URP or Built-in
- **URP (recommended):** the manifest includes `com.unity.render-pipelines.universal`. Create a URP Asset (Create > Rendering > URP Asset), assign it in Project Settings > Graphics and Quality, and turn on Bloom in a Global Volume so the lasers and red lights glow. glTFast PBR materials come in as URP Lit.
- **Built-in:** delete the `com.unity.render-pipelines.universal` line from `Packages/manifest.json`. The scripts work unchanged. For glow, add the Post Processing package and a Bloom effect. glTFast falls back to its Built-in shaders.

## Folder layout
```
Assets/
  Scripts/      C# behaviours (see below)
  Models/       Higgsfield-generated GLB props (textured, PBR)
  Higgsfield/   Concept images: Lasers, Lighting, RedLights, Bathroom, DJBooth, Speakers, LaserControl, Camera
Packages/manifest.json   includes glTFast and URP
```

## Scripts
| Script | What it does |
|---|---|
| WelcomeScreen | Title panel with a Play button that fades out and enters player mode |
| PlayerModeController | First-person WASD + mouse look, Shift sprint, Space jump, Esc back to menu |
| DungeonRaveLighting | Dark ambient + fog, beat-synced color cycling on rave lights |
| LaserShowController | LineRenderer laser beams with Fan, Scan, Spin and Strobe patterns, raycast-clipped |
| CameraDirector | Cinematic shot cycling / orbit for the welcome screen or spectator mode, with beat shake |
| DJBoothInteraction | Press E at the booth to cycle tracks and turn the show on or off |
| SpeakerPulse | Speaker cones pump to the bass (audio spectrum, BPM fallback) |
| BathroomLights | Flickering, buzzing fluorescent lights with random blackouts |
| RedLightStrobe | Red lights in Strobe, Breathe or rotating Beacon mode, with emissive materials |
| ScenePortal | Trigger that teleports the player or loads another scene |

## Quick scene setup
1. Drag GLBs from `Assets/Models/` into the scene and add colliders.
2. Player: a Capsule tagged `Player` with CharacterController + PlayerModeController, and the Main Camera as a child set as `cameraPivot`.
3. UI: Canvas > Panel (with a CanvasGroup) > Button. Add WelcomeScreen and link the panel, button and player.
4. Add a few Point/Spot lights to DungeonRaveLighting, an empty emitter with LaserShowController, and a DJ booth trigger with an AudioSource + DJBoothInteraction.

## 3D models (Assets/Models, GLB, PBR textures)
| File | Generator |
|---|---|
| dj_booth.glb | Meshy v6 text-to-3D (full, PBR) |
| speaker_stack.glb | Meshy v6 text-to-3D (full, PBR) |
| laser_projector.glb | Tripo 3D (textured, PBR) |
| moving_head_camera_rig.glb | Tripo 3D (textured, PBR) |
| red_industrial_wall_light.glb | Tripo 3D (textured, PBR) |
| grimy_toilet.glb | Tripo 3D (textured, PBR) |
| sink_with_mirror.glb | Tripo 3D (textured, PBR) |
| metal_crate_barrier.glb | Tripo 3D (textured, PBR) |

These import through glTFast (`com.unity.cloud.gltfast`, already in `Packages/manifest.json`). Each file is roughly 45 MB because the PBR textures are embedded, so you can lower the texture size in each texture's import settings. Scale and orientation can vary between models, so adjust each prefab's transform after dropping it in.

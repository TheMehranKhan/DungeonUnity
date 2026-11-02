# DungeonUnity

## Quick start (macOS)

One line in Terminal:

```
curl -fsSL https://raw.githubusercontent.com/TheMehranKhan/DungeonUnity/main/setup.command | zsh
```

Or clone the repo and double-click `setup.command` in Finder.

It clones or updates the project into `~/DungeonUnity`, downloads all models, textures and previews from the [assets-v1 release](https://github.com/TheMehranKhan/DungeonUnity/releases/tag/assets-v1) (resumable, checksum-verified), then opens Unity 2022.3 and runs `DungeonUnity.Editor.DungeonSetup.BuildScene`, which creates `Assets/Scenes/Welcome.unity` and `Assets/Scenes/Dungeon.unity` and adds them to Build Settings. Open `Welcome.unity` and press Play. You can rebuild any time from the menu **Dungeon > Build Scene**. Missing models are skipped with a warning.

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
  Scripts/      C# behaviours by folder (see below)
  Editor/       DungeonSetup scene builder
  Audio/        Generated WAV music, ambience and SFX
  Settings/     Presets (created on first build)
  Models/       Higgsfield-generated GLB props (textured, PBR)
  Higgsfield/   Concept images: Lasers, Lighting, RedLights, Bathroom, DJBooth, Speakers, LaserControl, Camera
Packages/manifest.json   includes glTFast and URP
```

## Controls
| Key | Action |
|---|---|
| WASD / arrows | Move |
| Mouse | Look |
| Shift | Sprint |
| Space | Jump |
| E | Interact (DJ booth, bathroom door, pills, toilet, exit door) |
| Esc or P | Pause menu (Resume, Settings, Quit to menu) |

Leave the mouse and keys alone for 20 seconds and AutoPilot walks you around the club; touch anything to take back control. Input uses the classic Input Manager (Unity's default in 2022.3).

## What the scene builder makes
`Dungeon > Build Scene` (or `-executeMethod DungeonUnity.Editor.DungeonSetup.BuildScene`) can be run again at any time. It rebuilds both scenes, reuses existing materials and presets, and skips missing models with one warning each.
- **Welcome**: title, Enter and Quit buttons, UI sounds and a scene fade.
- **Dungeon**: the room, stage, speakers, lasers, disco ball, rave, red and bathroom lights, smoke machines, a dancing crowd, a walking NPC with procedural legs, an interactive DJ booth, a bathroom door, a flushable toilet, trippy pills, an exit door, the player (footsteps, autopilot, trippy camera), a cinematic welcome camera, HUD prompt and crosshair, pause and settings menus, audio and beat sync, and URP post FX (bloom, vignette, chromatic aberration, colour). Post FX is skipped cleanly if URP is not active.

## Presets (ScriptableObjects in Assets/Settings)
- **DungeonConfig**: BPM, master/music/SFX/ambience volumes, feature toggles (lasers, fog, smoke, post FX, crowd, pills, autopilot), crowd count and area, and per-room palettes (light gradient, ambient, fog colour and density, laser colour, max intensity). Rooms switch when you walk into a RoomLightingZone (the bathroom has one).
- **CharacterRigConfig**: leg length, thickness, hip width and colour, stride length, leg swing, leg lift, body bob, lean, sway, step curve, blend speed and step height.

Change a preset, then run Build Scene again. Every script also exposes its own tuning in the Inspector, grouped under headers with tooltips and sliders.

## Scripts (namespace DungeonUnity)
| Folder | Script | What it does |
|---|---|---|
| Core | DungeonConfig, CharacterRigConfig | Preset assets described above |
| Core | GameSettings | Volumes, sensitivity and invert Y saved in PlayerPrefs |
| Core | InputCompat | Safe input wrapper with idle timer |
| Audio | AudioManager | Music, ambience and named SFX with variants, pitch jitter and fades |
| Audio | MusicBeatSync | Beat detection from the spectrum with a BPM clock fallback, exposes onBeat |
| Lighting | DungeonRaveLighting | Ambient, fog and rave lights in Cycle, Chase, RandomOnBeat or Solid mode, with room blending |
| Lighting | LaserShowController | Beams in Fan, Scan, Spin, Strobe or Tunnel patterns, raycast-clipped, beat-pulsed |
| Lighting | RedLightStrobe | Strobe, Breathe, Beacon or BeatFlash with emissive renderers |
| Lighting | BathroomLights | Flicker, blackouts and buzz |
| Lighting | SpeakerPulse | Cones pump to the bass with glow and squash |
| Lighting | DiscoBallSpin | Spinning ball with beat kicks and spot lights |
| Lighting | RoomLightingZone | Trigger that switches the room palette |
| FX | PostFXSetup | URP Volume with bloom, vignette, chromatic aberration and colour, set up by reflection so it is safe without URP |
| FX | TrippyEffect | FOV wobble, roll and hue overlay after a pill |
| FX | SmokeMachine | Low smoke particles with beat bursts |
| FX | SceneFader | Fade between scenes |
| Player | PlayerModeController | First-person movement, look, jump, sprint, head bob and step height |
| Player | PlayerFootsteps | Distance-based footstep sounds |
| Player | AutoPilot | Waypoint route (Sequential, PingPong, Random) with dwell and idle takeover |
| Player | CameraDirector | Cut, Blend, Orbit or Follow shots with FOV breathing and beat shake |
| Characters | CharacterRig, ProceduralWalk | Capsule legs and a speed-driven walk cycle |
| Characters | CrowdDancer | Bob, Sway, Twist, Jump or Headbang on the beat |
| Interaction | Interactable, InteractionPrompt | Look at it and press E, with an on-screen prompt |
| Interaction | DJBoothInteraction, BathroomDoor, PillPickup, ScenePortal, SoundInteractable | Booth music toggle, door, pills, scene exit, toilet flush |
| UI | WelcomeScreen, LoadSceneButton, PauseMenu, SettingsMenu, UISound | Menus and UI sounds |

## Sounds (Assets/Audio, generated by Tools/make_audio.py)
| File | Use |
|---|---|
| Music_Techno128_Loop | 30 s stereo 128 BPM techno loop (main music) |
| Loop_Kick128, Loop_Hats128 | Stems for remixing |
| Amb_Crowd | Club crowd ambience |
| Amb_RoomTone | Bathroom hum |
| SFX_Footstep_1 to 4 | Footsteps |
| SFX_LaserZap | Laser pattern change |
| SFX_DoorCreak | Bathroom and exit door |
| SFX_PillPickup | Pill pickup |
| SFX_ToiletFlush | Toilet |
| SFX_BassDrop | Music back on at the DJ booth |
| UI_Click, UI_Hover | Buttons |

All sounds are synthesised by code (no samples), so they are free to use. Run `python3 Tools/make_audio.py` (numpy and scipy) to regenerate them.

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

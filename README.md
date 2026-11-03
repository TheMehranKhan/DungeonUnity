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

Press Play and the intro camera flies from a wide shot down to your character, then self-play starts right away: AutoPilot tours every room (opening doors on the way). Touch any key or the mouse to take control; leave it alone for 20 seconds and the tour resumes. Turn the intro off with DungeonConfig > Enable Intro. Input uses the classic Input Manager (Unity's default in 2022.3).

## What the scene builder makes
`Dungeon > Build Scene` (or `-executeMethod DungeonUnity.Editor.DungeonSetup.BuildScene`) can be run again at any time. It rebuilds both scenes, reuses existing materials and presets, and skips missing models with one warning each.
- **Welcome**: title, Enter and Quit buttons, UI sounds and a scene fade.
- **Dungeon**: five rooms joined by doorways, corridors and doors: **Main Rave Hall**, **Bathroom** (door), **Red Light Corridor** (east), **Trippy Chill Room** (end of the corridor) and **VIP Backstage** (north, behind a door). Each room has a RoomZone that crossfades its own music and fades its lights and palette in. Also: the stage, stage, speakers, lasers, disco ball, rave, red and bathroom lights, a dancing crowd, a walking NPC with procedural legs, an interactive DJ booth, a bathroom door, a flushable toilet, trippy pills, an exit door, the player (footsteps, autopilot, trippy camera), a cinematic welcome camera, HUD prompt and crosshair, pause and settings menus, audio and beat sync, and URP post FX (bloom, vignette, chromatic aberration, colour). Post FX is skipped cleanly if URP is not active.

## Presets (ScriptableObjects in Assets/Settings)
- **DungeonConfig**: BPM, master/music/SFX volumes, feature toggles (lasers, post FX, crowd, pills, autopilot), crowd count and area, and per-room palettes (light gradient, ambient, laser colour, max intensity). Palettes are matched by name to each RoomZone; missing defaults are added on Build Scene without touching your edits. Also the Enable Intro toggle.
- **CharacterRigConfig**: leg length, thickness, hip width and colour, stride length, leg swing, leg lift, body bob, lean, sway, step curve, knee bend, blend speed and step height.

## Your own character (body + legs)
Open `Dungeon > Character Rig Setup`. Pick the Target (e.g. the Player or Wanderer) and drag your model (GLB/FBX/prefab) into Character model. Press **Auto-detect legs by name** or drag the pelvis, body and left/right upper and lower legs into the slots yourself, then press **Rig It**. It works with:
- rigged models (bones named thigh/upleg/calf/shin/knee etc., Mixamo names included),
- static GLBs whose legs are separate meshes (each part gets a hip/knee pivot),
- a single static body+legs mesh: it is split at the crotch (auto or set by slider) into body, upper and lower legs with hip and knee pivots; split meshes are saved in Assets/Generated/Rigs. Turn on Read/Write in the model import settings for this.
Rig It adds ProceduralWalk (and a CharacterController if missing). You can also set CharacterRig's Character slot directly in the Inspector; it rigs itself on Awake.

Change a preset, then run Build Scene again. Every script also exposes its own tuning in the Inspector, grouped under headers with tooltips and sliders.

## Scripts (namespace DungeonUnity)
| Folder | Script | What it does |
|---|---|---|
| Core | DungeonConfig, CharacterRigConfig | Preset assets described above |
| Core | GameSettings | Volumes, sensitivity and invert Y saved in PlayerPrefs |
| Core | InputCompat | Safe input wrapper with idle timer |
| Audio | AudioManager | Music, ambience and named SFX with variants, pitch jitter, fades and equal-power crossfades |
| Audio | MusicBeatSync | Beat detection from the spectrum with a BPM clock fallback, exposes onBeat |
| Lighting | DungeonRaveLighting | Ambient light and rave lights in Cycle, Chase, RandomOnBeat or Solid mode, with room blending |
| Lighting | LaserShowController | Beams in Fan, Scan, Spin, Strobe or Tunnel patterns, raycast-clipped, beat-pulsed |
| Lighting | RedLightStrobe | Strobe, Breathe, Beacon or BeatFlash with emissive renderers |
| Lighting | BathroomLights | Flicker, blackouts and buzz |
| Lighting | SpeakerPulse | Cones pump to the bass with glow and squash |
| Lighting | DiscoBallSpin | Spinning ball with beat kicks and spot lights |
| Lighting | RoomZone | Room trigger: crossfades music and ambience, fades room lights, applies the palette and lasers |
| FX | PostFXSetup | URP Volume with bloom, vignette, chromatic aberration and colour, set up by reflection so it is safe without URP |
| FX | TrippyEffect | FOV wobble, roll and hue overlay after a pill |
| FX | SceneFader | Fade between scenes |
| Player | PlayerModeController | First-person movement, look, jump, sprint, head bob and step height |
| Player | PlayerFootsteps | Distance-based footstep sounds |
| Player | AutoPilot | Waypoint route (Sequential, PingPong, Random) with dwell, self-play on start, idle takeover and door opening |
| Player | IntroSequence | Camera flies along an arc to the character on Play, then starts player mode and self-play |
| Player | CameraDirector | Cut, Blend, Orbit or Follow shots with FOV breathing and beat shake |
| Characters | CharacterRig, ProceduralWalk, RigUtility | Character slot, leg detection, static mesh splitting or capsule legs, and a speed-driven walk with knees |
| UI | RoomTitle | Shows the room name when you walk in |
| Editor | DungeonSetup, CharacterRigWindow | Build Scene; Character Rig Setup window and CharacterRig inspector buttons |
| Characters | CrowdDancer | Bob, Sway, Twist, Jump or Headbang on the beat |
| Interaction | Interactable, InteractionPrompt | Look at it and press E, with an on-screen prompt |
| Interaction | DJBoothInteraction, BathroomDoor, PillPickup, ScenePortal, SoundInteractable | Booth music toggle, door, pills, scene exit, toilet flush |
| UI | WelcomeScreen, LoadSceneButton, PauseMenu, SettingsMenu, UISound | Menus and UI sounds |

## Sounds (Assets/Audio, generated by Tools/make_audio.py)
| File | Use |
|---|---|
| Music_Techno128_Loop | 30 s stereo 128 BPM techno loop (main music) |
| Loop_Kick128, Loop_Hats128 | Stems for remixing |
| SFX_Footstep_1 to 4 | Footsteps |
| SFX_LaserZap | Laser pattern change |
| SFX_DoorCreak | Bathroom and exit door |
| SFX_PillPickup | Pill pickup |
| SFX_ToiletFlush | Toilet |
| SFX_BassDrop | Music back on at the DJ booth |
| UI_Click, UI_Hover | Buttons |

All sounds are synthesised by code (no samples), so they are free to use. Run `python3 Tools/make_audio.py` (numpy and scipy) to regenerate them.

### Music menu, dancing, new rooms
- **M** opens the music menu (pick any track, Prev/Next, Auto = each room's own music). **N** skips to the next track. Picking a track overrides room music until you press Auto.
- Per-room music: each `RoomZone` has `music` (clip) and `trackName` (a `MusicPlayer` track name, wins when set).
- Tracks (procedural, original): Techno Warehouse, Acid Squelch, Industrial Red Room, Velvet House, Ambient Chill, Festival Big Room. The bathroom plays `Music_Bathroom_Loop` (drips, bass through the wall, fluorescent hum). New SFX: Riser, Impact, CrowdCheer, PipeClank, DoorWhoosh.
- `ProceduralDance` (beat bounce, hip sway, head bob, twist, step-touch, rock, shuffle; switches every few bars). `CharacterRoster.danceRooms` (default Main Rave Hall, Festival) makes NPCs there dance instead of wander; per entry `dance` = Auto/Dance/Wander.
- `DancePair`: MaskedRaver and SpikeQueen dance facing each other on the Main Rave Hall floor, mirrored, on the beat, circling every few bars. Inspector: room, centerOffset, spacing, circle timing.
- New rooms: **Red Room** (industrial, through the Red Light Corridor's south door) and **Festival** (through the hall's south door: stage, speaker towers, lasers, a dancing crowd).
- Pills lie on the floor at `DungeonConfig.pillHeight`; floor props scale with `smallPropScale`.

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

## Lighting & rooms upgrade (URP)

`Dungeon > Improve Lighting & Rooms` (also run by Build Scene) gives every RoomZone a **RoomLightingRig**: key light with soft shadows, fill and rim lights, glowing neon strips on the floor and ceiling edges, a box-projected reflection probe and a light probe grid. It brightens ambient light (trilight), sets up the URP Volume (bloom, ACES tonemapping, exposure, colour, vignette), and adds metal **door frames** with glowing trims, a door light and a **DoorwayTrigger** that swings doors open smoothly when someone walks in and closes them when the doorway is empty (trims go pink when closed, green when open).
Tweak everything in `Assets/Settings/DungeonLightingConfig.asset` (sliders, colours, toggles, one look per room) and run the menu again. Right-click a RoomLightingRig > Rebuild Lighting to redo a single room. `Dungeon > Fix Magenta Materials` converts any leftover built-in materials to URP Lit.
Smoke, fog and the ambience sound layer were removed; music and room SFX stay.

## Adding characters

Drop GLB files into `Assets/Models/Characters` and run `Dungeon > Import Characters` (Build Scene does it too). Each one is added to the **CharacterRoster** in the Dungeon scene, auto-rigged (body + legs) by CharacterRig on Play, and wanders its room with ProceduralWalk + AutoPilot. Per character you can set room, count, height, yaw, rig source, leg split, walk speed and dwell time. For single-mesh models enable Read/Write on the import so the legs can be split; otherwise generated legs are used.

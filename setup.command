#!/bin/zsh
# DungeonUnity one-click setup (macOS). Double-click in Finder, or run:
#   curl -fsSL https://raw.githubusercontent.com/TheMehranKhan/DungeonUnity/main/setup.command | zsh
setopt nonomatch
set -u
REPO_URL="https://github.com/TheMehranKhan/DungeonUnity.git"
REL="https://github.com/TheMehranKhan/DungeonUnity/releases/download/assets-v1"
DEST="${DUNGEON_DIR:-$HOME/DungeonUnity}"
BIG=(dj_booth speaker_stack laser_projector moving_head_camera_rig red_industrial_wall_light grimy_toilet sink_with_mirror metal_crate_barrier)

say_() { print -P "%F{magenta}==>%f $*"; }
die()  { print -P "%F{red}ERROR:%f $*"; exit 1; }

# 1. clone or update
if [[ -d "$DEST/.git" ]]; then
  say_ "Updating $DEST"; git -C "$DEST" pull --ff-only || say_ "pull failed, continuing with local copy"
else
  command -v git >/dev/null || die "git not found (run: xcode-select --install)"
  say_ "Cloning into $DEST"; git clone --depth 1 "$REPO_URL" "$DEST" || die "clone failed"
fi
cd "$DEST" || die "cannot cd $DEST"
mkdir -p Assets/Models/Characters Assets/Models/Psychedelic Assets/Textures/Psychedelic .downloads

# 2. download with resume + retries
fetch() { # url out
  local i
  for i in 1 2 3; do
    curl -fL -C - --retry 10 --retry-delay 3 --retry-all-errors --connect-timeout 20 -o "$2" "$1" && return 0
    say_ "retrying $2 ($i)"; sleep 3
  done
  return 1
}
say_ "Downloading checksums"
curl -fsSL --retry 10 -o .downloads/SHA256SUMS.txt "$REL/SHA256SUMS.txt" || die "cannot reach release assets"

verify() { # name path
  local want got
  want=$(awk -v n="$1" '$2==n{print $1}' .downloads/SHA256SUMS.txt)
  [[ -f "$2" ]] || return 1
  got=$(shasum -a 256 "$2" | awk '{print $1}')
  [[ -n "$want" && "$want" == "$got" ]]
}

FAIL=()
for m in $BIG; do
  out="Assets/Models/$m.glb"
  if verify "$m.glb" "$out"; then say_ "ok  $m.glb (already here)"; continue; fi
  say_ "get $m.glb"
  fetch "$REL/$m.glb" "$out.part" && mv -f "$out.part" "$out"
  verify "$m.glb" "$out" && say_ "ok  $m.glb" || { rm -f "$out"; FAIL+=$m.glb; }
done

for c in MaskedRaver SpikeQueen; do
  out="Assets/Models/Characters/$c.glb"
  if verify "$c.glb" "$out"; then say_ "ok  $c.glb (already here)"; continue; fi
  say_ "get character $c.glb"
  fetch "$REL/$c.glb" "$out.part" && mv -f "$out.part" "$out"
  verify "$c.glb" "$out" && say_ "ok  $c.glb" || { rm -f "$out"; FAIL+=$c.glb; }
done

zip=.downloads/Psychedelic-pack.zip
if ! verify Psychedelic-pack.zip "$zip"; then
  say_ "get Psychedelic-pack.zip"; fetch "$REL/Psychedelic-pack.zip" "$zip.part" && mv -f "$zip.part" "$zip"
fi
if verify Psychedelic-pack.zip "$zip"; then
  unzip -oq "$zip" -d . && say_ "ok  Psychedelic pack (models, textures, previews)"
else FAIL+=Psychedelic-pack.zip; fi

n=$(ls Assets/Models/*.glb Assets/Models/Psychedelic/*.glb 2>/dev/null | wc -l | tr -d ' ')
t=$(ls Assets/Textures/Psychedelic/*.png 2>/dev/null | wc -l | tr -d ' ')
say_ "Have $n models (expect 19) plus $(ls Assets/Models/Characters/*.glb 2>/dev/null | wc -l | tr -d " ") characters (expect 2) and $t textures (expect 5)"
(( ${#FAIL} )) && say_ "Failed: ${FAIL[*]} (run setup.command again to resume)"

[[ "${SKIP_UNITY:-0}" == 1 ]] && { say_ "SKIP_UNITY=1, stopping before Unity"; exit 0; }

# 3. find Unity Editor (prefer Unity 6, then 2022.3, then any) incl. SlowTrash / external volumes
unity_ver_of() { # path -> version folder name (…/VER/Unity.app/Contents/MacOS/Unity)
  local p="$1"
  print -r -- "${${p:A}:h:h:h:h:t}"
}
find_unity() {
  local u ver best6="" best2022="" bestany="" v6="" v22=""
  local -a patterns=(
    /Volumes/SlowTrash/*/Unity.app/Contents/MacOS/Unity
    /Volumes/TrashCan*/Applications/Unity/*/Unity.app/Contents/MacOS/Unity
    /Volumes/*/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
    /Volumes/*/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
    /Volumes/*/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
    /Volumes/*/Applications/Unity/*/Unity.app/Contents/MacOS/Unity
    /Volumes/*/*/Unity.app/Contents/MacOS/Unity
    "$HOME/Unity/Hub/Editor"/*/Unity.app/Contents/MacOS/Unity
    /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
    /Applications/Unity/*/Unity.app/Contents/MacOS/Unity
    /Applications/Unity/Unity.app/Contents/MacOS/Unity
  )
  for pattern in $patterns; do
    for u in $~pattern(N); do
      [[ -x "$u" ]] || continue
      ver="$(unity_ver_of "$u")"
      case "$ver" in
        6000.*)
          if [[ -z "$best6" || "$ver" > "$v6" ]]; then best6="$u"; v6="$ver"; fi
          ;;
        2022.3.*)
          if [[ -z "$best2022" || "$ver" > "$v22" ]]; then best2022="$u"; v22="$ver"; fi
          ;;
        *)
          [[ -z "$bestany" ]] && bestany="$u"
          ;;
      esac
    done
  done
  if [[ -n "$best6" ]]; then print -r -- "$best6"
  elif [[ -n "$best2022" ]]; then print -r -- "$best2022"
  elif [[ -n "$bestany" ]]; then print -r -- "$bestany"
  fi
}
UNITY="$(find_unity)"
if [[ -n "$UNITY" ]]; then
  say_ "Opening in $(unity_ver_of "$UNITY") ($UNITY) and building the scene (first import takes a few minutes)"
  "$UNITY" -projectPath "$DEST" -executeMethod DungeonUnity.Editor.DungeonSetup.BuildScene >/dev/null 2>&1 &
  say_ "Unity is starting. When it finishes, open Assets/Scenes/Welcome.unity and press Play."
elif [[ -d "/Applications/Unity Hub.app" || -d "/Volumes/Unity Hub/Unity Hub.app" ]]; then
  say_ "No Unity Editor found. Install Unity 6 (or 2022.3) in Unity Hub - on SlowTrash / external drive is fine - then run this again (or Add project: $DEST, then menu Dungeon > Build Scene)."
  if [[ -d "/Applications/Unity Hub.app" ]]; then open -a "Unity Hub"
  else open "/Volumes/Unity Hub/Unity Hub.app"; fi
else
  say_ "Unity Hub not found. Get it at https://unity.com/download, install Unity 6 (or 2022.3), then run this again."
fi

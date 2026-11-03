#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonUnity.Editor
{
    /// Finds every GLB/FBX in Assets/Models/Characters and adds it to the scene's CharacterRoster, which rigs
    /// body + legs automatically (CharacterRig) and lets them wander the rooms as NPCs. Existing entries keep your edits.
    /// Menu: Dungeon/Import Characters. Build Scene runs it automatically.
    public static class CharacterImport
    {
        public const string Dir = "Assets/Models/Characters";

        [MenuItem("Dungeon/Import Characters")]
        public static void Run()
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();
            var roster = Object.FindObjectOfType<CharacterRoster>();
            if (!roster) roster = new GameObject("CharacterRoster").AddComponent<CharacterRoster>();
            if (!roster.rigConfig) roster.rigConfig = AssetDatabase.LoadAssetAtPath<CharacterRigConfig>("Assets/Settings/CharacterRigConfig.asset");
            var list = new List<CharacterRoster.Entry>(roster.characters ?? new CharacterRoster.Entry[0]);
            string[] rooms = { "Main Rave Hall", "Trippy Chill Room", "VIP Backstage", "Red Light Corridor", "Festival", "Red Room" };
            int added = 0;
            var files = Directory.GetFiles(Dir).Where(f => f.EndsWith(".glb") || f.EndsWith(".gltf") || f.EndsWith(".fbx")).OrderBy(f => f).ToArray();
            foreach (var f in files)
            {
                var path = f.Replace('\\', '/');
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) { Debug.LogWarning("[CharacterImport] Could not load " + path + " (is glTFast installed?)"); continue; }
                if (list.Any(e => e != null && e.model == prefab)) continue;
                list.Add(new CharacterRoster.Entry { label = Path.GetFileNameWithoutExtension(path), model = prefab, room = rooms[list.Count % rooms.Length], count = 1 });
                added++;
            }
            roster.characters = list.ToArray();
            EditorUtility.SetDirty(roster);
            // MaskedRaver + SpikeQueen dance together on the main floor (DancePair takes them out of the wander list at runtime)
            var pair = Object.FindObjectOfType<DancePair>();
            if (!pair) { pair = new GameObject("DancePair_MaskedRaver_SpikeQueen").AddComponent<DancePair>(); }
            pair.rigConfig = roster.rigConfig; pair.lightingConfig = roster.lightingConfig;
            foreach (var e in list)
            {
                if (e == null || !e.model) continue;
                if (e.label == pair.labelA) { pair.modelA = e.model; pair.height = e.height; pair.modelYawA = e.modelYaw; }
                if (e.label == pair.labelB) { pair.modelB = e.model; pair.modelYawB = e.modelYaw; }
            }
            EditorUtility.SetDirty(pair);
            EditorSceneManager.MarkSceneDirty(roster.gameObject.scene);
            EditorSceneManager.SaveScene(roster.gameObject.scene);
            Debug.Log($"[CharacterImport] {added} new character(s), {list.Count} total in the roster. Drop more GLBs into {Dir} and run Dungeon > Import Characters.");
        }
    }
}
#endif

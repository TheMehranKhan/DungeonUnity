using System.IO;
using UnityEditor;
using UnityEngine;

namespace DungeonUnity.EditorTools
{
    /// Dungeon > Character Rig Setup: pick a character (rigged or static body+legs GLB), auto-detect or assign legs, and Rig It.
    public class CharacterRigWindow : EditorWindow
    {
        GameObject target, model;
        Transform pelvis, body, lu, ll, ru, rl;
        CharacterRig.RigSource source = CharacterRig.RigSource.Auto;
        float legFraction = 0f, kneeFraction = 0.5f, modelHeight = 1.8f, modelYaw = 0f;
        bool splitKnees = true, addWalk = true, addController = true;
        CharacterRigConfig config;
        Vector2 scroll; string status = "";

        [MenuItem("Dungeon/Character Rig Setup")]
        public static void Open() { GetWindow<CharacterRigWindow>("Character Rig").minSize = new Vector2(340, 460); }

        public static void OpenFor(CharacterRig rig)
        {
            var w = GetWindow<CharacterRigWindow>("Character Rig");
            w.target = rig.gameObject; w.model = rig.characterModel; w.config = rig.config;
            w.pelvis = rig.pelvis; w.body = rig.body; w.lu = rig.leftUpperLeg; w.ll = rig.leftLowerLeg; w.ru = rig.rightUpperLeg; w.rl = rig.rightLowerLeg;
        }

        void OnSelectionChange() { if (!target && Selection.activeGameObject) { target = Selection.activeGameObject; Repaint(); } }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            target = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Target (player/NPC)", "Object that gets CharacterRig + ProceduralWalk."), target, typeof(GameObject), true);
            model = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Character model", "GLB/FBX/prefab or a scene object. Body+legs only is fine."), model, typeof(GameObject), true);
            source = (CharacterRig.RigSource)EditorGUILayout.EnumPopup("Rig source", source);
            modelHeight = EditorGUILayout.Slider("Model height (m)", modelHeight, 0f, 4f);
            modelYaw = EditorGUILayout.Slider("Model yaw", modelYaw, -180f, 180f);
            config = (CharacterRigConfig)EditorGUILayout.ObjectField("Rig config", config, typeof(CharacterRigConfig), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legs (leave empty to auto-detect / split)", EditorStyles.boldLabel);
            pelvis = Field("Pelvis", pelvis); body = Field("Body", body);
            lu = Field("Left upper leg", lu); ll = Field("Left lower leg", ll);
            ru = Field("Right upper leg", ru); rl = Field("Right lower leg", rl);
            if (GUILayout.Button("Auto-detect legs by name")) AutoDetect();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Static single-mesh split", EditorStyles.boldLabel);
            legFraction = EditorGUILayout.Slider(new GUIContent("Crotch height", "Fraction of model height. 0 = auto."), legFraction, 0f, 0.9f);
            splitKnees = EditorGUILayout.Toggle("Split knees", splitKnees);
            using (new EditorGUI.DisabledScope(!splitKnees)) kneeFraction = EditorGUILayout.Slider("Knee height", kneeFraction, 0.1f, 0.9f);

            EditorGUILayout.Space();
            addWalk = EditorGUILayout.Toggle("Add ProceduralWalk", addWalk);
            addController = EditorGUILayout.Toggle("Add CharacterController", addController);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!target))
                if (GUILayout.Button("Rig It", GUILayout.Height(32))) RigIt();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        static Transform Field(string label, Transform t) => (Transform)EditorGUILayout.ObjectField(label, t, typeof(Transform), true);

        void AutoDetect()
        {
            var root = model ? model.transform : (target ? target.transform : null);
            if (!root) { status = "Pick a model or target first."; return; }
            var d = RigUtility.Detect(root);
            if (model && !model.scene.IsValid()) { status = "Model is an asset: detected names will be resolved when you Rig It. " + Describe(d); return; }
            pelvis = d.pelvis ? d.pelvis : pelvis; lu = d.leftUpper ? d.leftUpper : lu; ll = d.leftLower ? d.leftLower : ll;
            ru = d.rightUpper ? d.rightUpper : ru; rl = d.rightLower ? d.rightLower : rl;
            if (!body) body = root;
            status = Describe(d);
        }

        static string Describe(RigUtility.Detected d) =>
            d.HasLegs ? $"Found legs: L {d.leftUpper.name}{(d.leftLower ? "/" + d.leftLower.name : "")}, R {d.rightUpper.name}{(d.rightLower ? "/" + d.rightLower.name : "")}."
                      : "No named legs found. Rig It will split a single static mesh at the crotch (or generate legs).";

        void RigIt()
        {
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(target), PrefabUnpackMode.Completely, InteractionMode.UserAction);
            if (model && model.scene.IsValid() && PrefabUtility.IsPartOfPrefabInstance(model))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(model), PrefabUnpackMode.Completely, InteractionMode.UserAction);

            var rig = target.GetComponent<CharacterRig>();
            if (!rig) rig = Undo.AddComponent<CharacterRig>(target);
            Undo.RegisterFullObjectHierarchyUndo(target, "Rig It");
            if (model && !model.scene.IsValid())
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, target.transform);
                if (!inst) inst = Instantiate(model, target.transform);
                if (PrefabUtility.IsPartOfPrefabInstance(inst)) PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.name = model.name;
                inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.Euler(0, modelYaw, 0);
                if (modelHeight > 0f)
                {
                    var b = RigUtility.RendererBounds(inst.transform);
                    if (b.size.y > 1e-3f) inst.transform.localScale *= modelHeight / b.size.y;
                    b = RigUtility.RendererBounds(inst.transform);
                    inst.transform.position += Vector3.up * (target.transform.position.y - b.min.y);
                }
                foreach (var c in inst.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
                Undo.RegisterCreatedObjectUndo(inst, "Rig It");
                rig.characterModel = inst;
            }
            else if (model) rig.characterModel = model;

            // Remove old generated legs so they don't double up.
            foreach (var n in new[] { "Hip_L", "Hip_R" }) { var old = target.transform.Find(n); if (old && old.Find(n + "_Leg")) Undo.DestroyObjectImmediate(old.gameObject); }

            rig.source = source; rig.modelHeight = modelHeight; rig.modelYaw = modelYaw;
            rig.legFraction = legFraction; rig.splitKnees = splitKnees; rig.kneeFraction = kneeFraction;
            if (config) rig.config = config;
            rig.pelvis = pelvis; rig.body = body; rig.leftUpperLeg = lu; rig.leftLowerLeg = ll; rig.rightUpperLeg = ru; rig.rightLowerLeg = rl;
            var split = rig.Build();
            if (split != null) SaveMeshes(split, rig.characterModel ? rig.characterModel.name : target.name);
            rig.buildOnAwake = false;

            if (addWalk && !target.GetComponent<ProceduralWalk>()) Undo.AddComponent<ProceduralWalk>(target);
            if (addController && !target.GetComponent<CharacterController>())
            {
                var cc = Undo.AddComponent<CharacterController>(target);
                var b = RigUtility.RendererBounds(target.transform);
                cc.height = Mathf.Max(1f, b.size.y); cc.radius = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 0.35f, 0.2f, 0.6f);
                cc.center = new Vector3(0, cc.height * 0.5f, 0);
            }
            lu = rig.leftUpperLeg; ll = rig.leftLowerLeg; ru = rig.rightUpperLeg; rl = rig.rightLowerLeg; body = rig.body; pelvis = rig.pelvis;
            EditorUtility.SetDirty(rig);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);
            Undo.CollapseUndoOperations(group);
            status = rig.IsRigged ? $"Rigged {target.name}: hips {rig.LeftHip.name}/{rig.RightHip.name}" + (rig.LeftKnee ? $", knees {rig.LeftKnee.name}/{rig.RightKnee.name}" : "") + (split != null ? ". Split meshes saved to Assets/Generated/Rigs." : ".")
                                  : "Could not find legs. Assign them above, or add a Rig Config to generate legs.";
        }

        static void SaveMeshes(RigUtility.SplitResult split, string name)
        {
            const string dir = "Assets/Generated/Rigs";
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Generated", "Rigs");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{Sanitize(name)}.asset");
            var holder = ScriptableObject.CreateInstance<CharacterRigConfig>(); holder.name = name + "_RigMeshes";
            AssetDatabase.CreateAsset(holder, path);
            foreach (var m in split.meshes) AssetDatabase.AddObjectToAsset(m, path);
            AssetDatabase.SaveAssets();
        }

        static string Sanitize(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }
    }

    [CustomEditor(typeof(CharacterRig))]
    public class CharacterRigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var rig = (CharacterRig)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(rig.IsRigged ? "Rigged." : "Not rigged yet. Use Auto-detect, or open the Rig Setup window and press Rig It.", rig.IsRigged ? MessageType.Info : MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto-detect legs"))
                {
                    Undo.RecordObject(rig, "Auto-detect legs");
                    var d = RigUtility.Detect(rig.characterModel && rig.characterModel.scene.IsValid() ? rig.characterModel.transform : rig.transform);
                    if (d.pelvis) rig.pelvis = d.pelvis; if (d.leftUpper) rig.leftUpperLeg = d.leftUpper; if (d.leftLower) rig.leftLowerLeg = d.leftLower;
                    if (d.rightUpper) rig.rightUpperLeg = d.rightUpper; if (d.rightLower) rig.rightLowerLeg = d.rightLower;
                    EditorUtility.SetDirty(rig);
                }
                if (GUILayout.Button("Open Rig Setup")) CharacterRigWindow.OpenFor(rig);
            }
        }
    }
}

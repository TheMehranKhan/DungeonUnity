using UnityEngine;

namespace DungeonUnity
{
    /// Walk/dance tuning shared by characters. Create via Assets > Create > DungeonUnity > Character Rig Config.
    [CreateAssetMenu(menuName = "DungeonUnity/Character Rig Config", fileName = "CharacterRigConfig")]
    public class CharacterRigConfig : ScriptableObject
    {
        [Header("Generated legs (for models without a skeleton)")]
        [Tooltip("Create simple capsule legs under the model.")] public bool generateLegs = true;
        [Range(0.2f, 1.5f)] public float legLength = 0.8f;
        [Range(0.03f, 0.3f)] public float legThickness = 0.09f;
        [Range(0.05f, 0.6f), Tooltip("Distance between the hips.")] public float hipWidth = 0.22f;
        public Color legColor = new Color(0.08f, 0.08f, 0.1f);

        [Header("Walk")]
        [Range(0.1f, 2f), Tooltip("Metres per full step cycle.")] public float strideLength = 0.9f;
        [Range(0f, 80f), Tooltip("Max leg swing angle.")] public float legSwingDegrees = 30f;
        [Range(0f, 0.4f), Tooltip("How high feet lift (knee-bend proxy).")] public float legLift = 0.08f;
        [Range(0f, 120f), Tooltip("Knee bend during the swing phase (rigs with knees).")] public float kneeBendDegrees = 35f;
        [Range(0f, 0.2f), Tooltip("Vertical body bob per step.")] public float bodyBob = 0.04f;
        [Range(0f, 20f), Tooltip("Forward lean at full speed.")] public float leanDegrees = 6f;
        [Range(0f, 15f), Tooltip("Side-to-side hip sway.")] public float swayDegrees = 3f;
        [Tooltip("Shape of a step (0..1 phase -> 0..1 lift).")] public AnimationCurve stepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Range(0.5f, 30f), Tooltip("Blend speed between idle and walk.")] public float blendSpeed = 8f;

        [Header("Step-up")]
        [Range(0f, 0.6f), Tooltip("Max step height a walker can climb.")] public float stepHeight = 0.3f;

        void OnValidate()
        {
            legLength = Mathf.Max(0.2f, legLength);
            strideLength = Mathf.Max(0.1f, strideLength);
            if (stepCurve == null || stepCurve.length < 2) stepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }
}

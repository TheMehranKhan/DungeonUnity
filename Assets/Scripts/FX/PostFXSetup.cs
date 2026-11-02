using System;
using UnityEngine;

namespace DungeonUnity
{
    /// Builds a global URP post-processing Volume (bloom, vignette, chromatic aberration, colour grading) at runtime.
    /// Uses reflection so it compiles and runs safely when URP is not installed (it just does nothing).
    public class PostFXSetup : MonoBehaviour
    {
        [Header("Bloom")]
        public bool bloom = true;
        [Range(0f, 10f)] public float bloomIntensity = 1.8f;
        [Range(0f, 2f)] public float bloomThreshold = 0.8f;
        [Range(0f, 1f)] public float bloomScatter = 0.7f;

        [Header("Vignette")]
        public bool vignette = true;
        [Range(0f, 1f)] public float vignetteIntensity = 0.35f;

        [Header("Chromatic aberration")]
        public bool chromaticAberration = true;
        [Range(0f, 1f)] public float chromaticIntensity = 0.15f;

        [Header("Colour")]
        public bool colorAdjustments = true;
        [Range(-100f, 100f)] public float saturation = 20f;
        [Range(-100f, 100f)] public float contrast = 15f;
        [Range(-180f, 180f)] public float hueShift = 0f;

        [Header("Cameras")]
        [Tooltip("Turn on post-processing on these cameras (URP).")] public Camera[] cameras = new Camera[0];

        Component volume; ScriptableObject profile;
        UnityEngine.Object colorAdjust, chroma;
        public bool Available { get; private set; }

        static Type T(string n) => Type.GetType(n);

        void Awake() { Build(); }

        public void Build()
        {
            var volumeT = T("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            var profileT = T("UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime");
            if (volumeT == null || profileT == null || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
            { Debug.Log("[PostFXSetup] URP not active, skipping post FX."); return; }
            try
            {
                volume = GetComponent(volumeT);
                if (!volume) volume = gameObject.AddComponent(volumeT);
                SetMember(volume, "isGlobal", true);
                SetMember(volume, "priority", 10f);
                profile = ScriptableObject.CreateInstance(profileT);
                if (!SetMember(volume, "sharedProfile", profile)) SetMember(volume, "profile", profile);
                const string U = ", Unity.RenderPipelines.Universal.Runtime";
                if (bloom) { var b = Add("UnityEngine.Rendering.Universal.Bloom" + U); Set(b, "intensity", bloomIntensity); Set(b, "threshold", bloomThreshold); Set(b, "scatter", bloomScatter); }
                if (vignette) { var v = Add("UnityEngine.Rendering.Universal.Vignette" + U); Set(v, "intensity", vignetteIntensity); }
                if (chromaticAberration) { chroma = Add("UnityEngine.Rendering.Universal.ChromaticAberration" + U); Set(chroma, "intensity", chromaticIntensity); }
                if (colorAdjustments) { colorAdjust = Add("UnityEngine.Rendering.Universal.ColorAdjustments" + U); Set(colorAdjust, "saturation", saturation); Set(colorAdjust, "contrast", contrast); Set(colorAdjust, "hueShift", hueShift); }
                var camDataT = T("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData" + U);
                if (camDataT != null)
                    foreach (var cam in cameras)
                    {
                        if (!cam) continue;
                        var data = cam.GetComponent(camDataT);
                        if (!data) data = cam.gameObject.AddComponent(camDataT);
                        SetMember(data, "renderPostProcessing", true);
                    }
                Available = true;
            }
            catch (Exception e) { Debug.LogWarning("[PostFXSetup] Could not build post FX: " + e.Message); }
        }

        static bool SetMember(object target, string name, object value)
        {
            if (target == null) return false;
            var t = target.GetType();
            var p = t.GetProperty(name);
            if (p != null && p.CanWrite) { p.SetValue(target, value); return true; }
            var f = t.GetField(name);
            if (f != null) { f.SetValue(target, value); return true; }
            return false;
        }

        UnityEngine.Object Add(string typeName)
        {
            var t = T(typeName); if (t == null || !profile) return null;
            var m = profile.GetType().GetMethod("Add", new[] { typeof(Type), typeof(bool) });
            return m?.Invoke(profile, new object[] { t, true }) as UnityEngine.Object;
        }

        static void Set(UnityEngine.Object comp, string field, float value)
        {
            if (!comp) return;
            var param = comp.GetType().GetField(field)?.GetValue(comp); if (param == null) return;
            param.GetType().GetProperty("overrideState")?.SetValue(param, true);
            param.GetType().GetProperty("value")?.SetValue(param, value);
        }

        /// Used by TrippyEffect to push hue/chromatic while tripping. Safe no-op without URP.
        public void SetTrip(float hue, float chromaticExtra)
        {
            if (!Available) return;
            Set(colorAdjust, "hueShift", Mathf.Clamp(hueShift + hue, -180f, 180f));
            Set(chroma, "intensity", Mathf.Clamp01(chromaticIntensity + chromaticExtra));
        }

        void OnDestroy() { if (profile) Destroy(profile); }
    }
}

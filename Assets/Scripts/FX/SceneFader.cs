using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonUnity
{
    /// Fades to black between scenes. Survives scene loads; creates its own overlay canvas.
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }
        [Range(0.05f, 5f)] public float fadeSeconds = 0.6f;
        public Color fadeColor = Color.black;
        [Tooltip("Fade in automatically when a scene starts.")] public bool fadeInOnLoad = true;

        CanvasGroup group; bool busy;

        public static SceneFader Ensure()
        {
            if (Instance) return Instance;
            var go = new GameObject("SceneFader"); return go.AddComponent<SceneFader>();
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.alpha = fadeInOnLoad ? 1f : 0f;
            var img = new GameObject("Fade", typeof(RectTransform)).AddComponent<Image>(); img.transform.SetParent(transform, false);
            var rt = img.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; img.color = fadeColor; img.raycastTarget = false;
            SceneManager.sceneLoaded += OnLoaded;
        }

        void Start() { if (fadeInOnLoad) StartCoroutine(Fade(0f)); }
        void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; if (Instance == this) Instance = null; }
        void OnLoaded(Scene s, LoadSceneMode m) { if (fadeInOnLoad && this) StartCoroutine(Fade(0f)); }

        public static void LoadScene(string sceneName) { Ensure().FadeTo(sceneName); }

        public void FadeTo(string sceneName)
        {
            if (busy) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName)) { Debug.LogWarning("[SceneFader] Scene not in Build Settings: " + sceneName); return; }
            StartCoroutine(FadeAndLoad(sceneName));
        }

        IEnumerator FadeAndLoad(string sceneName)
        {
            busy = true; Time.timeScale = 1f;
            yield return Fade(1f);
            SceneManager.LoadScene(sceneName);
            busy = false;
        }

        IEnumerator Fade(float target)
        {
            if (!group) yield break;
            group.blocksRaycasts = target > 0.5f;
            while (!Mathf.Approximately(group.alpha, target))
            {
                group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
                yield return null;
            }
            group.blocksRaycasts = false;
        }
    }
}

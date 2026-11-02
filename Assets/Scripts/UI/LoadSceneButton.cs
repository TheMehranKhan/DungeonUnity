using UnityEngine;

namespace DungeonUnity
{
    /// Hook a Button's onClick to Load(). Fades via SceneFader.
    public class LoadSceneButton : MonoBehaviour
    {
        public string sceneName = "Dungeon";
        public void Load() { if (!string.IsNullOrEmpty(sceneName)) SceneFader.LoadScene(sceneName); }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

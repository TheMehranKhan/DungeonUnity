using UnityEngine;
using UnityEngine.SceneManagement;

/// Loads a scene by name. Hook to a UI Button (used by the Welcome scene's Player Mode button).
public class LoadSceneButton : MonoBehaviour
{
    public string sceneName = "Dungeon";
    public void Load() { SceneManager.LoadScene(sceneName); }
}

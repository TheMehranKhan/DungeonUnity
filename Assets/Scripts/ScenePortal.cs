using UnityEngine;
using UnityEngine.SceneManagement;

/// Trigger that teleports the player to a target point or loads another scene (e.g. dance floor to bathroom).
public class ScenePortal : MonoBehaviour
{
    public string playerTag = "Player"; public Transform destination; public string sceneToLoad;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!string.IsNullOrEmpty(sceneToLoad)) { SceneManager.LoadScene(sceneToLoad); return; }
        if (!destination) return;
        var cc = other.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        other.transform.SetPositionAndRotation(destination.position, destination.rotation);
        if (cc) cc.enabled = true;
    }
}

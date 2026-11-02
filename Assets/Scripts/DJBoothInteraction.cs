using UnityEngine;
using UnityEngine.Events;

/// Walk up to the DJ booth and press E to cycle tracks / toggle the show. Needs a trigger collider.
[RequireComponent(typeof(AudioSource))]
public class DJBoothInteraction : MonoBehaviour
{
    public AudioClip[] tracks; public KeyCode interactKey = KeyCode.E; public string playerTag = "Player";
    public LaserShowController[] lasers; public DungeonRaveLighting raveLighting;
    public UnityEvent onTrackChanged; public GameObject promptUI;
    AudioSource src; bool inRange; int track = -1;

    void Awake() { src = GetComponent<AudioSource>(); src.loop = true; if (promptUI) promptUI.SetActive(false); }
    void OnTriggerEnter(Collider o) { if (o.CompareTag(playerTag)) { inRange = true; if (promptUI) promptUI.SetActive(true); } }
    void OnTriggerExit(Collider o) { if (o.CompareTag(playerTag)) { inRange = false; if (promptUI) promptUI.SetActive(false); } }

    void Update() { if (inRange && Input.GetKeyDown(interactKey)) NextTrack(); }

    public void NextTrack()
    {
        if (tracks == null || tracks.Length == 0) return;
        track = (track + 1) % (tracks.Length + 1);
        bool off = track == tracks.Length;
        if (off) src.Stop(); else { src.clip = tracks[track]; src.Play(); }
        foreach (var l in lasers) if (l) l.enabled = !off;
        if (raveLighting) raveLighting.enabled = !off;
        onTrackChanged?.Invoke();
    }
}

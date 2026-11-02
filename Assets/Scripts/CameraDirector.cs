using UnityEngine;

/// Cinematic camera that cuts or blends between shot points (works on the welcome screen or as spectator mode).
public class CameraDirector : MonoBehaviour
{
    public Camera cam; public Transform[] shots; public Transform lookTarget;
    public float shotSeconds = 6f, blendSpeed = 1.5f, orbitSpeed = 5f; public bool smoothBlend = true;
    public bool beatShake = true; public float bpm = 128f, shakeAmount = 0.03f;
    int index; float timer;

    void Reset() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (!cam || shots == null || shots.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= shotSeconds) { timer = 0f; index = (index + 1) % shots.Length; if (!smoothBlend) Snap(); }
        var s = shots[index];
        if (lookTarget) s.RotateAround(lookTarget.position, Vector3.up, orbitSpeed * Time.deltaTime);
        var ct = cam.transform;
        ct.position = Vector3.Lerp(ct.position, s.position, Time.deltaTime * blendSpeed);
        Quaternion rot = lookTarget ? Quaternion.LookRotation(lookTarget.position - ct.position) : s.rotation;
        ct.rotation = Quaternion.Slerp(ct.rotation, rot, Time.deltaTime * blendSpeed);
        if (beatShake) ct.position += Random.insideUnitSphere * shakeAmount * Mathf.Pow(1f - (Time.time * bpm / 60f % 1f), 4f);
    }

    void Snap() { cam.transform.SetPositionAndRotation(shots[index].position, shots[index].rotation); }
    public void NextShot() { timer = shotSeconds; }
}

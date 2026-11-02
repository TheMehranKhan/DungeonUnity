using UnityEngine;

/// Simple first-person controller. Toggle with SetPlayerMode. Esc returns the cursor.
[RequireComponent(typeof(CharacterController))]
public class PlayerModeController : MonoBehaviour
{
    public Transform cameraPivot;
    public float moveSpeed = 4f, sprintMultiplier = 1.8f, mouseSensitivity = 2f, gravity = -9.81f, jumpHeight = 1f;
    public GameObject welcomePanel;
    CharacterController cc; float pitch, vy; bool active;

    void Awake() { cc = GetComponent<CharacterController>(); }

    public void SetPlayerMode(bool on)
    {
        active = on;
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }

    void Update()
    {
        if (!active) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { SetPlayerMode(false); if (welcomePanel) welcomePanel.SetActive(true); return; }
        transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
        if (cameraPivot) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        if (cc.isGrounded) { vy = -1f; if (Input.GetButtonDown("Jump")) vy = Mathf.Sqrt(jumpHeight * -2f * gravity); }
        vy += gravity * Time.deltaTime;
        cc.Move((Vector3.ClampMagnitude(move, 1f) * speed + Vector3.up * vy) * Time.deltaTime);
    }
}

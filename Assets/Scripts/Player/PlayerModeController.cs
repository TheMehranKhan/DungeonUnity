using UnityEngine;

namespace DungeonUnity
{
    /// First-person controller (legacy Input Manager via InputCompat). SetPlayerMode(true) to take control.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerModeController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Pitch pivot holding the camera.")] public Transform cameraPivot;
        [Tooltip("Shown again when leaving player mode.")] public GameObject welcomePanel;
        public CharacterRigConfig rigConfig;

        [Header("Movement")]
        [Range(0.5f, 15f)] public float moveSpeed = 4f;
        [Range(1f, 4f)] public float sprintMultiplier = 1.8f;
        [Range(1f, 40f), Tooltip("Ground acceleration.")] public float acceleration = 12f;
        [Range(-40f, -1f)] public float gravity = -9.81f;
        [Range(0f, 5f)] public float jumpHeight = 1f;
        [Range(0f, 1f), Tooltip("Max step height (CharacterController.stepOffset). Overridden by rigConfig.stepHeight if set.")] public float stepHeight = 0.3f;

        [Header("Look")]
        [Tooltip("Use the saved sensitivity from Settings instead of the value below.")] public bool useSavedSensitivity = true;
        [Range(0.1f, 10f)] public float mouseSensitivity = 2f;
        [Range(10f, 90f)] public float pitchLimit = 85f;
        public bool invertY = false;

        [Header("Head bob")]
        public bool headBob = true;
        [Range(0f, 0.2f)] public float bobAmount = 0.04f;
        [Range(0.5f, 4f), Tooltip("Bobs per metre.")] public float bobFrequency = 1.6f;

        [Header("State")]
        [Tooltip("Start in player mode (skip the welcome screen).")] public bool startActive = false;

        CharacterController cc; float pitch, vy, bobPhase; Vector3 velocity, pivotBase;
        public bool Active { get; private set; }
        /// When true (AutoPilot driving or paused) input is ignored.
        public bool ExternalControl { get; set; }
        public bool Paused { get; set; }
        public Vector3 Velocity => velocity;
        public bool IsGrounded => cc && cc.isGrounded;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot) pivotBase = cameraPivot.localPosition;
            ApplyStep();
            GameSettings.Changed += LoadSettings; LoadSettings();
        }

        void OnDestroy() { GameSettings.Changed -= LoadSettings; }
        void OnValidate() { ApplyStep(); }
        void Start() { if (startActive) SetPlayerMode(true); }

        void ApplyStep()
        {
            if (!cc) cc = GetComponent<CharacterController>();
            if (!cc) return;
            float h = rigConfig ? rigConfig.stepHeight : stepHeight;
            cc.stepOffset = Mathf.Clamp(h, 0f, Mathf.Max(0f, cc.height - 0.05f));
        }

        void LoadSettings()
        {
            if (useSavedSensitivity) mouseSensitivity = GameSettings.Sensitivity;
            invertY = GameSettings.InvertY;
        }

        public void SetPlayerMode(bool on)
        {
            Active = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
            if (!on && welcomePanel) welcomePanel.SetActive(true);
        }

        void Update()
        {
            InputCompat.Poll();
            if (!Active || Paused || ExternalControl) { ApplyGravityOnly(); return; }
            var look = InputCompat.Look;
            transform.Rotate(0f, look.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch + (invertY ? 1f : -1f) * look.y * mouseSensitivity, -pitchLimit, pitchLimit);
            var mv = InputCompat.Move;
            Vector3 wish = Vector3.ClampMagnitude(transform.right * mv.x + transform.forward * mv.y, 1f) * moveSpeed * (InputCompat.Sprint ? sprintMultiplier : 1f);
            velocity = Vector3.Lerp(velocity, wish, Time.deltaTime * acceleration);
            if (cc.isGrounded) { vy = -1f; if (InputCompat.JumpDown) vy = Mathf.Sqrt(jumpHeight * -2f * gravity); }
            vy += gravity * Time.deltaTime;
            cc.Move((velocity + Vector3.up * vy) * Time.deltaTime);
            UpdatePivot();
        }

        void ApplyGravityOnly()
        {
            if (!cc || !cc.enabled || ExternalControl) return;
            if (cc.isGrounded) vy = -1f; else vy += gravity * Time.deltaTime;
            cc.Move(Vector3.up * vy * Time.deltaTime);
        }

        void UpdatePivot()
        {
            if (!cameraPivot) return;
            Vector3 bob = Vector3.zero;
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            if (headBob && cc.isGrounded && speed > 0.1f)
            {
                bobPhase += speed * bobFrequency * Time.deltaTime * Mathf.PI * 2f;
                bob = new Vector3(Mathf.Cos(bobPhase * 0.5f) * bobAmount * 0.5f, Mathf.Sin(bobPhase) * bobAmount, 0f);
            }
            cameraPivot.localPosition = pivotBase + bob;
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, cameraPivot.localEulerAngles.z);
        }

        /// Lets AutoPilot steer while keeping look pitch level.
        public void DriveExternally(Vector3 worldVelocity, Quaternion facing, float turnSpeed)
        {
            if (!cc || !cc.enabled) return;
            velocity = worldVelocity;
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, Time.deltaTime * turnSpeed);
            if (cc.isGrounded) vy = -1f; else vy += gravity * Time.deltaTime;
            cc.Move((worldVelocity + Vector3.up * vy) * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 0f, Time.deltaTime * 2f);
            UpdatePivot();
        }
    }
}

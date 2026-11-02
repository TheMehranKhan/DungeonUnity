using UnityEngine;

namespace DungeonUnity
{
    /// Single place that reads input. Uses the legacy Input Manager, which works with
    /// Project Settings > Player > Active Input Handling set to "Input Manager (Old)" or "Both".
    public static class InputCompat
    {
        public static float LastInputTime { get; private set; }
        static int lastFrame = -1;

        public static Vector2 Move => new Vector2(Axis("Horizontal"), Axis("Vertical"));
        public static Vector2 Look => new Vector2(Axis("Mouse X"), Axis("Mouse Y"));
        public static bool Sprint => Key(KeyCode.LeftShift) || Key(KeyCode.RightShift);
        public static bool JumpDown => Down(KeyCode.Space);
        public static bool InteractDown(KeyCode k) => Down(k);
        public static bool PauseDown => Down(KeyCode.Escape) || Down(KeyCode.P);

        public static bool Key(KeyCode k) { bool v = Input.GetKey(k); if (v) Touch(); return v; }
        public static bool Down(KeyCode k) { bool v = Input.GetKeyDown(k); if (v) Touch(); return v; }

        public static float Axis(string name)
        {
            float v;
            try { v = Input.GetAxis(name); } catch (System.ArgumentException) { return 0f; }
            if (Mathf.Abs(v) > 0.01f) Touch();
            return v;
        }

        /// Call once per frame to register any key press as activity (for AutoPilot idle detection).
        public static void Poll()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (Input.anyKey) Touch();
        }

        static void Touch() { LastInputTime = Time.unscaledTime; }
        public static float IdleSeconds => Time.unscaledTime - LastInputTime;
    }
}

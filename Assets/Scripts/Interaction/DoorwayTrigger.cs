using System.Collections.Generic;
using UnityEngine;

namespace DungeonUnity
{
    /// Trigger box in a doorway. Opens the linked door when the player or an NPC walks in and closes it
    /// a few seconds after the doorway is empty. Also recolours the door trims (closed / open).
    [RequireComponent(typeof(BoxCollider))]
    public class DoorwayTrigger : MonoBehaviour
    {
        [Tooltip("Door to open. Empty = trims only.")] public BathroomDoor door;
        [Tooltip("Who can open it. Empty tag list = anything with a CharacterController.")] public string[] tags = { "Player" };
        public bool npcsOpenToo = true;
        [Range(0f, 30f), Tooltip("Close after the doorway is empty this long (0 = never).")] public float autoCloseSeconds = 4f;

        [Header("Trims")]
        public Renderer[] trims = new Renderer[0];
        public Light trimLight;
        [ColorUsage(false, true)] public Color closedColor = new Color(1f, 0.15f, 0.6f) * 2.5f;
        [ColorUsage(false, true)] public Color openColor = new Color(0.2f, 1f, 0.5f) * 2.5f;
        [Range(0.05f, 5f)] public float colorBlendSeconds = 0.4f;

        readonly HashSet<Collider> inside = new HashSet<Collider>();
        float emptySince = -1f, blend; MaterialPropertyBlock mpb;

        void Reset() { var b = GetComponent<BoxCollider>(); b.isTrigger = true; }
        void Awake() { GetComponent<BoxCollider>().isTrigger = true; mpb = new MaterialPropertyBlock(); }

        bool Accept(Collider c)
        {
            foreach (var t in tags) if (!string.IsNullOrEmpty(t) && c.CompareTag(t)) return true;
            return npcsOpenToo && c.GetComponent<CharacterController>();
        }

        void OnTriggerEnter(Collider c)
        {
            if (!Accept(c)) return;
            inside.Add(c); emptySince = -1f;
            if (door) door.Open();
        }

        void OnTriggerExit(Collider c)
        {
            if (inside.Remove(c) && inside.Count == 0) emptySince = Time.time;
        }

        void Update()
        {
            inside.RemoveWhere(c => !c || !c.gameObject.activeInHierarchy);
            if (door && autoCloseSeconds > 0f && inside.Count == 0 && emptySince > 0f && Time.time - emptySince > autoCloseSeconds && door.IsOpen)
            { door.Close(); emptySince = -1f; }

            bool open = door ? door.IsOpen : inside.Count > 0;
            blend = Mathf.MoveTowards(blend, open ? 1f : 0f, Time.deltaTime / colorBlendSeconds);
            var col = Color.Lerp(closedColor, openColor, blend);
            foreach (var r in trims)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", col); r.SetPropertyBlock(mpb);
            }
            if (trimLight)
            {
                float mx = Mathf.Max(0.001f, Mathf.Max(col.r, Mathf.Max(col.g, col.b)));
                trimLight.color = new Color(col.r / mx, col.g / mx, col.b / mx);
            }
        }
    }
}

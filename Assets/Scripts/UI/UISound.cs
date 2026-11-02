using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonUnity
{
    /// Click / hover sounds for any UI element (uses AudioManager named SFX).
    public class UISound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public string hoverSfx = "UIHover";
        public string clickSfx = "UIClick";
        [Range(0f, 1f)] public float volume = 0.8f;

        public void OnPointerEnter(PointerEventData e) { if (!string.IsNullOrEmpty(hoverSfx)) AudioManager.Play(hoverSfx, null, volume); }
        public void OnPointerClick(PointerEventData e) { if (!string.IsNullOrEmpty(clickSfx)) AudioManager.Play(clickSfx, null, volume); }
    }
}

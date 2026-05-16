using UnityEngine;
using UnityEngine.UI;

namespace TheAlchemistsCrypt.UI
{
    /// <summary>
    /// Utility to adjust the UI to the safe area (avoiding notches and home indicators).
    /// </summary>
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea = new Rect(0, 0, 0, 0);

        private void Awake()
        {
            panel = GetComponent<RectTransform>();
            Refresh();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            Rect safeArea = Screen.safeArea;

            if (safeArea != lastSafeArea)
            {
                lastSafeArea = safeArea;
                ApplySafeArea(safeArea);
            }
        }

        private void ApplySafeArea(Rect r)
        {
            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
        }
    }
}

using UnityEngine;

namespace Swordstellation.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            Rect reportedSafeArea = Screen.safeArea;
            Rect safeArea = reportedSafeArea;
            if (safeArea.width <= Mathf.Epsilon ||
                safeArea.height <= Mathf.Epsilon)
            {
                safeArea = new Rect(0f, 0f, screenWidth, screenHeight);
            }

            safeArea.xMin = Mathf.Clamp(safeArea.xMin, 0f, screenWidth);
            safeArea.yMin = Mathf.Clamp(safeArea.yMin, 0f, screenHeight);
            safeArea.xMax = Mathf.Clamp(safeArea.xMax, 0f, screenWidth);
            safeArea.yMax = Mathf.Clamp(safeArea.yMax, 0f, screenHeight);

            RectTransform rectTransform = (RectTransform)transform;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            _lastSafeArea = reportedSafeArea;
            _lastScreenSize = new Vector2Int(screenWidth, screenHeight);

            Debug.Log(
                $"Safe area applied: reported={reportedSafeArea}, " +
                $"resolved={safeArea}, screen={screenWidth}x{screenHeight}");
        }
    }
}

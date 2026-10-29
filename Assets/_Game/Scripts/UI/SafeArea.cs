using System;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>
    /// Fits this RectTransform inside <see cref="Screen.safeArea"/> on the top and sides.
    /// The bottom is left at the screen edge so the drawer background can run under the
    /// home indicator; the drawer pads its own buttons with <see cref="BottomInset"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        /// <summary>Insets in canvas (reference) units.</summary>
        public static float TopInset { get; private set; }
        public static float BottomInset { get; private set; }
        public static event Action Changed;

        private RectTransform rectTransform;
        private Canvas canvas;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastScale;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea
                || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height
                || !Mathf.Approximately(lastScale, canvas.scaleFactor))
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            float width = Screen.width, height = Screen.height;
            if (width <= 0 || height <= 0) return;

            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastScale = canvas.scaleFactor;

            rectTransform.anchorMin = new Vector2(safe.xMin / width, 0);
            rectTransform.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            float scale = Mathf.Max(lastScale, 0.0001f);
            TopInset = (height - safe.yMax) / scale;
            BottomInset = safe.yMin / scale;
            Changed?.Invoke();
        }
    }
}

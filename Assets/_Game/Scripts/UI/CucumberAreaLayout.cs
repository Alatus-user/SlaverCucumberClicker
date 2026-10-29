using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>
    /// Resizes the cucumber area to the space between the top bar and the drawer and keeps
    /// the big cucumber centered and as large as fits.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CucumberAreaLayout : MonoBehaviour
    {
        [SerializeField] private DrawerController drawer;
        [SerializeField] private CucumberButton cucumber;
        [SerializeField] private RectTransform tapPowerLabel;

        [SerializeField] private float topBarHeight = 160;
        [Range(0, 1)] [SerializeField] private float widthFraction = 0.65f;
        [SerializeField] private float labelHeight = 90;
        [SerializeField] private float padding = 30;

        private RectTransform rectTransform;
        private float drawerHeight;
        private Vector2 lastSize;
        private bool layingOut;

        private void Awake() => rectTransform = (RectTransform)transform;

        private void OnEnable()
        {
            drawer.OnHeightChanged += HandleHeight;
            drawer.OnStateChanged += HandleState;
            HandleHeight(drawer.VisibleHeight);
            HandleState(drawer.State);
        }

        private void OnDisable()
        {
            drawer.OnHeightChanged -= HandleHeight;
            drawer.OnStateChanged -= HandleState;
        }

        private void HandleHeight(float visibleHeight)
        {
            drawerHeight = visibleHeight;
            Layout();
        }

        private void HandleState(DrawerState state) => cucumber.SetInteractable(state != DrawerState.Expanded);

        // Not OnRectTransformDimensionsChange: Layout() resizes this rect, which would fire
        // that callback synchronously and recurse forever. Poll for parent resizes instead.
        private void LateUpdate()
        {
            if (rectTransform.rect.size != lastSize) Layout();
        }

        private void Layout()
        {
            if (layingOut) return;
            layingOut = true;
            try
            {
                LayoutInternal();
            }
            finally
            {
                layingOut = false;
            }
        }

        private void LayoutInternal()
        {
            var offsetMin = new Vector2(0, drawerHeight);
            var offsetMax = new Vector2(0, -topBarHeight);
            if (rectTransform.offsetMin != offsetMin) rectTransform.offsetMin = offsetMin;
            if (rectTransform.offsetMax != offsetMax) rectTransform.offsetMax = offsetMax;

            Rect area = rectTransform.rect;
            lastSize = area.size;
            float size = Mathf.Max(0, Mathf.Min(area.width * widthFraction, area.height - labelHeight - padding * 2));

            var cucumberRect = (RectTransform)cucumber.transform;
            cucumberRect.sizeDelta = new Vector2(size, size);
            cucumberRect.anchoredPosition = new Vector2(0, labelHeight * 0.5f);

            if (tapPowerLabel != null)
            {
                tapPowerLabel.anchoredPosition = new Vector2(0, labelHeight * 0.5f - size * 0.5f - labelHeight * 0.5f);
                tapPowerLabel.gameObject.SetActive(size > labelHeight);
            }
        }
    }
}

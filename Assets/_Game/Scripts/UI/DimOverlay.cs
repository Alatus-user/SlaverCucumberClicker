using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Darkens the cucumber area as the drawer opens past Half. Tap it to drop back to Half.</summary>
    [RequireComponent(typeof(Image))]
    public class DimOverlay : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private DrawerController drawer;
        [Range(0, 1)] [SerializeField] private float maxAlpha = 0.4f;

        private Image image;

        private void Awake() => image = GetComponent<Image>();

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

        private void HandleHeight(float height)
        {
            // Stays clear up to Half so the cucumber remains readable and tappable.
            float t = Mathf.InverseLerp(drawer.HalfVisible, drawer.ExpandedVisible, height);
            Color c = image.color;
            c.a = t * maxAlpha;
            image.color = c;
        }

        private void HandleState(DrawerState state) => image.raycastTarget = state == DrawerState.Expanded;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (drawer.State == DrawerState.Expanded) drawer.SnapTo(DrawerState.Half);
        }
    }
}

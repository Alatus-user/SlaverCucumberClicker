using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CucumberClicker.UI
{
    public enum DrawerState { Collapsed, Half, Expanded }

    /// <summary>
    /// Bottom drawer tray that holds every menu. Slides between three snap states.
    /// Other UI listens to <see cref="OnHeightChanged"/> instead of reading this RectTransform.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DrawerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform root;
        [Tooltip("Scrolling content area; padded so buttons never sit under the home indicator.")]
        [SerializeField] private RectTransform content;
        [SerializeField] private GrabHandle grabHandle;

        [Header("Layout (reference px)")]
        [SerializeField] private float collapsedHeight = 220;
        [SerializeField] private float topBarHeight = 160;
        [SerializeField] private float expandedGap = 20;
        [Range(0, 1)] [SerializeField] private float halfScreenFraction = 0.5f;

        [Header("Feel")]
        [SerializeField] private float flingVelocity = 1500;
        [SerializeField] private float skipHalfVelocity = 4000;
        [SerializeField] private float snapDuration = 0.25f;
        [Range(0, 1)] [SerializeField] private float rubberBand = 0.3f;

        public DrawerState State { get; private set; } = DrawerState.Collapsed;
        public float VisibleHeight { get; private set; }
        public bool IsDragging { get; private set; }
        public float NormalizedOpen => Mathf.InverseLerp(CollapsedVisible, ExpandedVisible, VisibleHeight);

        /// <summary>Visible height in canvas units, measured from the bottom of the screen.</summary>
        public event Action<float> OnHeightChanged;
        public event Action<DrawerState> OnStateChanged;

        public float CollapsedVisible => collapsedHeight + SafeArea.BottomInset;
        public float ExpandedVisible => Mathf.Max(CollapsedVisible, root.rect.height - topBarHeight - expandedGap);
        public float HalfVisible =>
            Mathf.Clamp((root.rect.height + SafeArea.TopInset) * halfScreenFraction, CollapsedVisible, ExpandedVisible);

        private RectTransform rectTransform;
        private Canvas canvas;

        private float dragRaw;
        private float velocity;
        private float lastDragTime;

        private bool snapping;
        private float snapFrom, snapTo, snapTime;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void Start() => SetVisible(CollapsedVisible, force: true);

        public float HeightFor(DrawerState state) => state switch
        {
            DrawerState.Half => HalfVisible,
            DrawerState.Expanded => ExpandedVisible,
            _ => CollapsedVisible,
        };

        public void SnapTo(DrawerState state, bool animate = true)
        {
            SetState(state);
            snapFrom = VisibleHeight;
            snapTo = HeightFor(state);
            snapTime = 0;
            snapping = animate;
            if (!animate) SetVisible(snapTo);
        }

        /// <summary>Android back button. Returns false when already collapsed.</summary>
        public bool HandleBack()
        {
            switch (State)
            {
                case DrawerState.Expanded: SnapTo(DrawerState.Half); return true;
                case DrawerState.Half: SnapTo(DrawerState.Collapsed); return true;
                default: return false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            snapping = false;
            IsDragging = true;
            dragRaw = VisibleHeight;
            velocity = 0;
            lastDragTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            float dy = eventData.delta.y / canvas.scaleFactor;
            float dt = Mathf.Max(Time.unscaledTime - lastDragTime, 1f / 240f);
            velocity = Mathf.Lerp(velocity, dy / dt, 0.6f);
            lastDragTime = Time.unscaledTime;

            dragRaw += dy;
            SetVisible(RubberBand(dragRaw));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            IsDragging = false;

            // Finger held still before lifting: no fling.
            if (Time.unscaledTime - lastDragTime > 0.1f) velocity = 0;
            SnapTo(PickTarget(VisibleHeight, velocity));
        }

        private DrawerState PickTarget(float height, float v)
        {
            float speed = Mathf.Abs(v);
            if (speed > skipHalfVelocity) return v > 0 ? DrawerState.Expanded : DrawerState.Collapsed;

            if (speed > flingVelocity)
            {
                if (v > 0)
                {
                    if (height < HalfVisible - 1) return DrawerState.Half;
                    return DrawerState.Expanded;
                }
                if (height > HalfVisible + 1) return DrawerState.Half;
                return DrawerState.Collapsed;
            }

            DrawerState nearest = DrawerState.Collapsed;
            float best = float.MaxValue;
            foreach (DrawerState s in new[] { DrawerState.Collapsed, DrawerState.Half, DrawerState.Expanded })
            {
                float distance = Mathf.Abs(HeightFor(s) - height);
                if (distance < best)
                {
                    best = distance;
                    nearest = s;
                }
            }
            return nearest;
        }

        private float RubberBand(float raw)
        {
            float min = CollapsedVisible, max = ExpandedVisible;
            if (raw < min) return min - (min - raw) * rubberBand;
            if (raw > max) return max + (raw - max) * rubberBand;
            return raw;
        }

        private void LateUpdate()
        {
            if (snapping)
            {
                // Target is re-read every frame so a safe-area or rotation change mid-snap still lands correctly.
                snapTo = HeightFor(State);
                snapTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(snapTime / snapDuration);
                float eased = 1 - Mathf.Pow(1 - t, 3);
                SetVisible(Mathf.LerpUnclamped(snapFrom, snapTo, eased));

                if (t >= 1)
                {
                    snapping = false;
                    Haptics.Tick();
                }
            }
            else if (!IsDragging)
            {
                // Keep pinned to the current state if the screen or safe area changed.
                SetVisible(HeightFor(State));
            }

            ApplyLayout();
        }

        private void SetState(DrawerState state)
        {
            if (state == State) return;
            State = state;
            OnStateChanged?.Invoke(state);
        }

        private void SetVisible(float height, bool force = false)
        {
            if (!force && Mathf.Abs(height - VisibleHeight) < 0.01f) return;
            VisibleHeight = height;
            ApplyLayout();
            OnHeightChanged?.Invoke(height);
        }

        private void ApplyLayout()
        {
            // The drawer is always full expanded size; sliding it is cheaper than resizing
            // because the scroll content doesn't need a layout rebuild every frame.
            float fullHeight = Mathf.Max(ExpandedVisible, VisibleHeight);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, fullHeight);
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, VisibleHeight - fullHeight);

            if (content != null) content.offsetMin = new Vector2(content.offsetMin.x, SafeArea.BottomInset);
        }

        /// <summary>Briefly glow the grab handle, e.g. when a jar finishes while collapsed.</summary>
        public void GlowHandle()
        {
            if (grabHandle != null) grabHandle.Glow();
        }
    }
}

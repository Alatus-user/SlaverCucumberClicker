using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>The big tappable cucumber. Every finger that touches it counts as a tap.</summary>
    [RequireComponent(typeof(Image))]
    public class CucumberButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private FloatingTextSpawner floatingText;
        [SerializeField] private UIBurst burst;
        [SerializeField] private TextMeshProUGUI tapPowerLabel;
        [SerializeField] private Color floatingTextColor = new Color(0.2f, 0.55f, 0.15f);
        [Range(0, 1)] [SerializeField] private float alphaHitThreshold = 0.1f;

        [Header("Animation")]
        [SerializeField] private float squashDuration = 0.15f;
        [SerializeField] private float idleDelay = 3f;
        [SerializeField] private float breathAmount = 0.03f;
        [SerializeField] private float breathPeriod = 2f;

        private Image image;
        private bool interactable = true;
        private float squashTime = float.MaxValue;
        private float lastTapTime;
        private float breathTime;
        private double shownTapPower = -1;

        private void Awake()
        {
            image = GetComponent<Image>();
            // Transparent corners of the sprite don't count as taps (texture must be Read/Write enabled).
            image.alphaHitTestMinimumThreshold = alphaHitThreshold;
            lastTapTime = Time.unscaledTime;
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (image == null) image = GetComponent<Image>();
            image.raycastTarget = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable) return;

            double gained = GameManager.Instance.Tap();
            if (gained <= 0) return;

            squashTime = 0;
            lastTapTime = Time.unscaledTime;
            floatingText.SpawnAtScreen(eventData.position, Strings.FloatingGain(NumberFormatter.Format(gained)), floatingTextColor);
            burst.PlayAtScreen(eventData.position);
            Haptics.Light();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            squashTime += dt;

            // Squash-and-stretch: 1 -> 0.9 -> 1.05 -> 1 on Y, opposite on X.
            float s = SquashCurve(squashTime / squashDuration);
            var scale = new Vector3(2 - s, s, 1);

            if (Time.unscaledTime - lastTapTime > idleDelay)
            {
                breathTime += dt;
                float breath = 1 + breathAmount * (0.5f - 0.5f * Mathf.Cos(breathTime / breathPeriod * Mathf.PI * 2));
                scale *= breath;
            }
            else
            {
                breathTime = 0;
            }

            transform.localScale = scale;

            double tapPower = GameManager.Instance.TapPower;
            if (tapPowerLabel != null && tapPower != shownTapPower)
            {
                shownTapPower = tapPower;
                tapPowerLabel.text = Strings.PerTap(NumberFormatter.Format(tapPower));
            }
        }

        private static float SquashCurve(float t)
        {
            if (t >= 1) return 1;
            if (t < 1f / 3) return Mathf.SmoothStep(1f, 0.9f, t * 3);
            if (t < 2f / 3) return Mathf.SmoothStep(0.9f, 1.05f, t * 3 - 1);
            return Mathf.SmoothStep(1.05f, 1f, t * 3 - 2);
        }
    }
}

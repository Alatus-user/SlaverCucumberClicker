using TMPro;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>Number label that counts smoothly toward its target and pulses its icon on increase.</summary>
    public class CountUpLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform icon;
        [SerializeField] private float countDuration = 0.25f;
        [SerializeField] private float pulseDuration = 0.15f;
        [SerializeField] private float pulseScale = 1.2f;

        private double shown, from, target;
        private float countTime = float.MaxValue;
        private float pulseTime = float.MaxValue;
        private string shownText;
        private Color baseColor;
        private Color flashColor;
        private float flashTime = float.MaxValue, flashDuration;

        public TextMeshProUGUI Label => label;

        private void Awake() => baseColor = label.color;

        public void SetTarget(double value)
        {
            if (value == target) return;
            if (value > target) pulseTime = 0;
            from = shown;
            target = value;
            countTime = 0;
        }

        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashDuration = duration;
            flashTime = 0;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            countTime += dt;
            float t = Mathf.Clamp01(countTime / countDuration);
            float eased = 1 - (1 - t) * (1 - t) * (1 - t);
            shown = t >= 1 ? target : from + (target - from) * eased;

            string text = NumberFormatter.Format(shown);
            if (text != shownText)
            {
                shownText = text;
                label.text = text;
            }

            if (icon != null)
            {
                pulseTime += dt;
                float p = Mathf.Clamp01(pulseTime / pulseDuration);
                icon.localScale = Vector3.one * (1 + (pulseScale - 1) * Mathf.Sin(p * Mathf.PI));
            }

            flashTime += dt;
            float f = flashDuration > 0 ? Mathf.Clamp01(flashTime / flashDuration) : 1;
            label.color = Color.Lerp(flashColor, baseColor, f);
        }
    }
}

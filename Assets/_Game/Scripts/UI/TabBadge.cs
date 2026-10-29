using TMPro;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>Number badge and/or red "new" dot on a drawer tab.</summary>
    public class TabBadge : MonoBehaviour
    {
        [SerializeField] private RectTransform countBadge;
        [SerializeField] private TextMeshProUGUI countLabel;
        [SerializeField] private GameObject redDot;
        [SerializeField] private float popDuration = 0.35f;

        private int count;
        private float popTime = float.MaxValue;

        public void SetCount(int value)
        {
            if (value == count) return;
            if (count == 0 && value > 0) popTime = 0;
            count = value;

            if (countBadge == null) return;
            countBadge.gameObject.SetActive(value > 0);
            if (value > 0) countLabel.text = value > 99 ? "99+" : value.ToString();
        }

        public void SetDot(bool visible)
        {
            if (redDot != null && redDot.activeSelf != visible) redDot.SetActive(visible);
        }

        private void Update()
        {
            if (countBadge == null || !countBadge.gameObject.activeSelf) return;
            popTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(popTime / popDuration);
            // Ease-out-back: grows from 0, overshoots a little, settles at 1.
            const float c1 = 1.70158f, c3 = c1 + 1;
            float q = p - 1;
            float s = 1 + c3 * q * q * q + c1 * q * q;
            countBadge.localScale = Vector3.one * s;
        }
    }
}
